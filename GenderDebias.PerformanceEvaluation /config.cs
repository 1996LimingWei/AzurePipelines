namespace GenderDebias.PerformanceEvaluation
{
    internal class Config
    {
        public string WorkFolderPath { get; set; } = "";
        public string TestRootPath { get; set; } = "";
        public int TopN { get; set; } = -1;
        public string EndPoint { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public Config(string envType, string workFolderPath, string testRootPath, int topN)
        {
            var settings = ConfigurationManager.AppSettings;
            WorkFolderPath = workFolderPath;
            TestRootPath = testRootPath;
            TopN = topN;

            EndPoint = Sanity.RequiresNotNoll(settings[$"GenderDebias_Endpoint_{envType}"], $"Missing endpoint setting for {envType}");
            SecretKey = Sanity.RequiresNotNoll(settings[$"GenderDebias_Key_{envType}"], $"Missing key setting for {envType}");
        }
    }
}
