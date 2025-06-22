using GenderDebias.Common;
using System.Configuration;

namespace GenderDebias.PerformanceEvaluation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Usage:");            
            Console.WriteLine("\t>GenderDebias.PerformanceEvaluation.exe [EnvType] [TestRootPath] [WorkFolderPath] [TopN]");
            Console.WriteLine("\tEnvType is REQUIED, which can be prod, int, dev, prodeu, prodapc");
            Console.WriteLine("\tTestRootPath is optional, which is the root folder of the test files.");
            Console.WriteLine("\tWorkFolder path is optional, which is the folder path of the output.");
            Console.WriteLine("\tTopN is optional, which is how many sentences will be translated in each test file, for debugging; Use -1 to translate the entire file.");

            var r = GetArgument(args);

            Config cfg = new Config(r.envType,r.workFolderPath,r.testRootPath,r.topN);
            ApiConnectHelper.Init(r.envType, cfg.EndPoint, cfg.SecretKey);

            CallGenderDebias cgd = new CallGenderDebias(cfg);
            cgd.CallGenderDebiasOnTestFiles();

            EvaluationResult er = new EvaluationResult();
            er.EvalAllFiles(cfg.WorkFolderPath);
        }

        static (string envType, string workFolderPath, string testRootPath, int topN) GetArgument(string[] args)
        {
            string envType = "prod";
            string workFolderPath = "Output";
            string testRootPath = "..\\..\\..\\..\\..\\test_sets";
            int topN = -1;

            if (args.Length >= 4)            
                topN = int.Parse(args[3]);            
            else
                Console.WriteLine($"Missing top N setting, use -1 as default.");            

            if (args.Length >= 3)
                testRootPath = args[2];
            else
                Console.WriteLine($"Missing test root path setting, use ..\\..\\..\\..\\..\\test_sets as default.");

            if (args.Length >= 2)
                workFolderPath = args[1];
            else
                Console.WriteLine($"Missing work folder path setting, use Output as default.");

            if (args.Length >= 1)
                envType = args[0];
            else
            {
                Console.WriteLine($"Missing EnvType argument, which is required. Exit.");
                Environment.Exit(1);
            }

            return (envType, workFolderPath, testRootPath, topN);
        }
    }
}
