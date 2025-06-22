namespace GenderDebias.PerformanceEvaluation
{
    internal class EvaluationResult
    {
        List<string> ErrorList = new List<string>();
        List<TestResult> SummaryList = new List<TestResult>();
        private int HypCount(GenderDebiasOutputTgt tgt)
        {
            int n = 0;
            if (!string.IsNullOrWhiteSpace(tgt.Neutral))
                n++;
            if (!string.IsNullOrWhiteSpace(tgt.Masculine))
                n++;
            if (!string.IsNullOrWhiteSpace(tgt.Feminine))
                n++;
            return n;
        }

        enum ErrorType
        {
            Success,
            // All the types below are hard error, the numbers won't be counted in precision/recall/accuracy.

            // No hyp file.
            MissingHypFile,

            // Src and hyp has different sizes.
            SrcHypCountMismatch,

            // Hyp format error.
            HypFormatError,

            // Translation failed.
            TranslationFail,

            // The output count 
            OutputCountMismatch,

            // For positive test set, the hyp doesn't match the tgt.
            PosHypMismatch,
        }
        private (bool? outputMasculine, string output) Matches(string target, string n, string m, string f)
        {
            if (target.Trim() == n.Trim())
            {
                if (m.Length > 0)
                    return (true, m.Trim());
                else if (f.Length > 0)
                    return (false, f.Trim());
            }
            if (target.Trim() == m.Trim())
            {
                if (n.Trim().Length > 0)
                    return (false, n.Trim());
                else
                    return (false, f.Trim());
            }
            if (target.Trim() == f.Trim())
            {
                if (m.Trim().Length > 0)
                    return (true, m.Trim());
                else
                    return (true, n.Trim());
            }
            // If none of the above, then there should be error here.
            return (null, "");
        }
        private (int tPos, int tNeg, int fPos, int fNeg) CalcConfusionMatrixNeg(int n)
        {
            // If output is not 1 or 2, then hard error.
            Sanity.Requires(n == 1 || n == 2, ErrorType.OutputCountMismatch.ToString());

            // If output is 1, then true negative, otherwise false positive.
            if (n == 1)
                return (0, 1, 0, 0);
            else
                return (0, 0, 1, 0);
        }
        private (int tPos, int tNeg, int fPos, int fNeg) CalcConfusionMatrixPos(string target, string reference, 
            GenderDebiasOutputTgt hypothesis, bool? hypMasculine, int n)
        {
            // If output is not 1 or 2, then hard error.
            Sanity.Requires(n == 1 || n == 2, ErrorType.OutputCountMismatch.ToString());
            // If only 1 output, then false negative.
            if (n == 1)
                return (0, 0, 0, 1);

            var matches = Matches(target, hypothesis.Neutral, hypothesis.Masculine, hypothesis.Feminine);
            if (matches.outputMasculine == null)
                throw new Exception(ErrorType.PosHypMismatch.ToString());

            // If there is gender issue, and gender is incorrect, then false positive(by overriding the definition).
            if (hypMasculine != null && matches.outputMasculine != hypMasculine)
                return (0, 0, 1, 0);

            // If the output is the same as refrence, then true positive, otherwise false positive(by overriding the definition).
            if (matches.output == reference.Trim())
                return (1, 0, 0, 0);
            else
                return (0, 0, 1, 0);
        }
        private void CalculatePrecisionRecallAccuracy(string lang, string category, bool negative, string sourcePath, string targetPath, string referencePath, string hypothesisPath, bool? hypMasculine)
        {
            if (!File.Exists(hypothesisPath))
            {
                ErrorList.Add($"{ErrorType.MissingHypFile}\t{lang}\t{category}\t");
                return;
            }

            string[] hypothesisArray = File.ReadAllLines(hypothesisPath);
            string[] sourceArray = File.ReadAllLines(sourcePath);
            if (hypothesisArray.Length != sourceArray.Length)
            {
                ErrorList.Add($"{ErrorType.SrcHypCountMismatch}\t{lang}\t{category}\t");
                return;
            }

            string[] targetArray = File.ReadAllLines(targetPath);

            // Negative set doesn't have reference.
            string[] referenceArray = negative
                ? new string[0]
                : File.ReadAllLines(referencePath);

            TestResult tr = new TestResult
            {
                Lang = lang,
                Category = category,
                Negative = negative,
                Total = hypothesisArray.Length,
                TruePositive = 0,
                TrueNegative = 0,
                FalsePositive = 0,
                FalseNegative = 0,
            };

            for (int i = 0; i < hypothesisArray.Length; i++)
            {
                string hyp = hypothesisArray[i];
                var hypSplit = hyp.Split('\t');
                try
                {
                    Sanity.Requires(hypSplit.Length == 4, ErrorType.HypFormatError.ToString());
                    Sanity.Requires(hypSplit[0] == "true", ErrorType.TranslationFail.ToString());
                    GenderDebiasOutputTgt hypothesis = new GenderDebiasOutputTgt
                    {
                        Feminine = hypSplit[1],
                        Masculine = hypSplit[2],
                        Neutral = hypSplit[3],
                    };
                    int n = HypCount(hypothesis);

                    var t = negative
                        ? CalcConfusionMatrixNeg(n)
                        : CalcConfusionMatrixPos(targetArray[i], referenceArray[i], hypothesis, hypMasculine, n);

                    tr.TruePositive += t.tPos;
                    tr.TrueNegative += t.tNeg;
                    tr.FalsePositive += t.fPos;
                    tr.FalseNegative += t.fNeg;
                    tr.ValidCount++;
                }
                catch (Exception e)
                {
                    ErrorList.Add($"{e.Message}\t{lang}\t{category}\t{hyp}");
                }
            }
            SummaryList.Add(tr);
        }
