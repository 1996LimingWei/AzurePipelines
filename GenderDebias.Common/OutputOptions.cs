namespace GenderDebias.Common
{
    public class OutputOptions
    {
        [JsonIgnore]
        public List<int> best_hyp_idx { get; set; } = new List<int>();
        public object aborted_reason { get; set; } = new object();
        public List<List<string>> hypotheses { get; set; } = new List<List<string>>();
        public List<List<double>> hypForwardScores { get; set; } = new List<List<double>>();
        public List<List<double>> hypReverseScores { get; set; } = new List<List<double>>();
        public List<List<bool>> hypHasOkWordScores { get; set; } = new List<List<bool>>();
        public List<string> hypGenders { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"" +
                $"aborted_reason={aborted_reason}, \t best_hyp_idx = {(best_hyp_idx == null ? string.Empty : string.Join(" ", best_hyp_idx))}" +
                $" \t hypotheses={(hypotheses != null && hypotheses[0] != null ? string.Join(" ", hypotheses.Select(h => string.Join(",", h))) : String.Empty)}" +
                $" \t hypForwardScores={(hypForwardScores != null && hypForwardScores[0] != null ? string.Join(" ", hypForwardScores.Select(h => string.Join(",", h))) : String.Empty)}" +
                $" \t hypReverseScores={(hypReverseScores != null && hypReverseScores[0] != null ? string.Join(" ", hypReverseScores.Select(h => string.Join(",", h))) : String.Empty)}" +
                $" \t hypHasOkWordScores={(hypHasOkWordScores != null && hypHasOkWordScores[0] != null ? string.Join(" ", hypHasOkWordScores.Select(h => string.Join(",", h))) : String.Empty)}" +
                $" \t hypGenders={(hypGenders != null ? string.Join(" ", hypGenders) : String.Empty)}" +
                "";
        }

    }
}
