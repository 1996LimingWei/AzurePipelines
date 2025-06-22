namespace GenderDebias.Common
{
    public class GenderDebiasOutputTgt
    {
        public string Feminine { get; set; } = "";
        public string Masculine { get; set; } = "";
        public string Neutral { get; set; } = "";

        public override string ToString()
        {
            return $"{Feminine}\t{Masculine}\t{Neutral}";
        }
    }
}
