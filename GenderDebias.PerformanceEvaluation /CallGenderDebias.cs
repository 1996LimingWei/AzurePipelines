namespace GenderDebias.PerformanceEvaluation
{
    internal class CallGenderDebias
    {
        const int RETRY_MAX = 3;
        public Config Cfg { get; set; }
        private int TopN { get; set; } = -1;
        public CallGenderDebias(Config cfg)
        {
            Cfg = cfg;
        }
        private void CallGenderDebiasOnTestFile(TestSets testSets, string lang, string category)
        {
            Console.WriteLine($"Processing {lang}-{category}");

            string srcPath = Path.Combine(Cfg.TestRootPath, testSets.source);
            string tgtPath = Path.Combine(Cfg.TestRootPath, testSets.orig_tgt);            

            string type = testSets.negative ? "Neg" : "Pos";

            string outputFolderPath = Path.Combine(Cfg.WorkFolderPath, ApiConnectHelper.EnvType, lang, type, category);
            Directory.CreateDirectory(outputFolderPath);

            string hypPath = Path.Combine(outputFolderPath, $"hyp.txt");
            string copiedSrcPath = Path.Combine(outputFolderPath, $"src.txt");
            string copiedTgtPath = Path.Combine(outputFolderPath, $"tgt.txt");

            if (TopN == -1)
            {
                File.Copy(srcPath, copiedSrcPath, true);
                File.Copy(tgtPath, copiedTgtPath, true);
            }
            else
            {
                // If top n is set specifically, then only translate the top n lines.
                // For debug only.
                File.WriteAllLines(copiedSrcPath, File.ReadLines(srcPath).Take(TopN));
                File.WriteAllLines(copiedTgtPath, File.ReadLines(tgtPath).Take(TopN));
            }

            if (!string.IsNullOrWhiteSpace(testSets.refs))
            {
                string refPath = Path.Combine(Cfg.TestRootPath, testSets.refs);
                string copiedRefPath = Path.Combine(outputFolderPath, "ref.txt");
                if (TopN == -1)
                    File.Copy(refPath, copiedRefPath, true);
                else
                    File.WriteAllLines(copiedRefPath, File.ReadLines(refPath).Take(TopN));
            }

            Console.WriteLine($"\tCall gender debias on {lang}-{category}");
            CallGenderDebiasOnTestFile(srcPath, tgtPath, "3", hypPath, "en", lang);
            Console.WriteLine($"\tProcessing on {lang}-{category} is done.");
            Console.WriteLine();
        }
        private void CallGenderDebiasOnTestFile(string srcPath, string tgtPath, string timeOut, string hypFilePath, string src, string tgt)
        {
            var srcArray = File.ReadAllLines(srcPath);
            var tgtArray = File.ReadAllLines(tgtPath);
            if (TopN == -1)
                TopN = srcArray.Length;
            else if (TopN > srcArray.Length)
            {
                TopN = srcArray.Length;
            }
            string[] hypArray = new string[TopN];
            Console.WriteLine($"\t{TopN} testcases in total.");
            /*
             * The parallel is line based, the reason is the size between different files may be large.
             * The major time consuming step is call API, the cost on the parallel is worthy.
             * In practice, sequential uses around 8 hours; parallel uses around 1 hour.
             */
            
            Parallel.For(0, TopN, new ParallelOptions { MaxDegreeOfParallelism = 10 }, i =>
            {
                HttpResponseMessage response = ApiConnectHelper.CallGdbSvcApi(timeOut, srcArray[i], src, tgtArray[i], tgt);
                var headers = response.Headers.ToString().Replace(Environment.NewLine, "\t");

                int retry = 0;

                while (true)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var output = JsonSerializer.Deserialize<GenderDebiasOutput>(result);
                        if (output is null)
                        {
                            retry++;
                            continue;
                        }
                        else
                        {
                            hypArray[i] = $"true\t{output.tgt}";                            
                            break;
                        }
                    }
                    else
                    {
                        retry++;
                    }
                    if (retry >= RETRY_MAX)
                    {
                        hypArray[i] = $"false\t{srcArray[i]}";
                        break;
                    }
                }
            });

            File.WriteAllLines(hypFilePath, hypArray);
        }
        public void CallGenderDebiasOnTestFiles()
        {
            // Read the test set json file, and translate the file based on its values.
            Console.WriteLine();
            Console.WriteLine($"Start to test the performance on gender debias...");
            Console.WriteLine();
            string jsonPath = Path.Combine(Cfg.TestRootPath, "test_sets.json");
            string jsonContent = File.ReadAllText(jsonPath);
            var dict = Sanity.RequiresNotNoll(JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, TestSets>>>(jsonContent));
            TopN = Cfg.TopN;
            foreach (var languageItem in dict)
            {
                string lang = languageItem.Key;
                foreach (var categoryItem in languageItem.Value)
                {
                    string category = categoryItem.Key;
                    var testSets = categoryItem.Value;
                    CallGenderDebiasOnTestFile(testSets, lang, category);
                }
            }
        }
