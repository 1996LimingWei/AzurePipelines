namespace GenderDebias.Common
{
    public static class ApiConnectHelper
    {
        public static string EnvType { get; set; } = "";
        private static string EndPoint { get; set; } = "";
        private static string SecretValue { get; set; } = "";

        static ApiConnectHelper()
        {
        }

        public static void Init(string envType, string endPoint, string secretKey, bool forceRefresh=false)
        {
            if (SecretValue != "" && !forceRefresh)
                return;
            Console.WriteLine($"Start to retrieve the connection key...");
            EnvType = envType;
            EndPoint = endPoint;
            SecretClient client = new SecretClient(new Uri("https://mttestvault.vault.azure.net/"), new DefaultAzureCredential());
            SecretValue = client.GetSecretAsync(secretKey).ConfigureAwait(false).GetAwaiter().GetResult().Value.Value;
            Console.WriteLine($"Connection key retrieved.");
        }
              public static HttpResponseMessage CallGdbSvcApi(string timeout, string? srcText, string? srcLanguage, string? tgtText, string? tgtLanguage, bool debug = true, bool logInput = true)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (p, q, r, s) => true,
            };
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SecretValue);
                client.BaseAddress = new Uri(EndPoint);
                client.DefaultRequestHeaders.Add("X-MT-TextType", "Text");
                client.DefaultRequestHeaders.Add("X-MT-Timeout", timeout);
                var traceId = Guid.NewGuid().ToString();
                client.DefaultRequestHeaders.Add("X-MT-CorrelationId", traceId);

                string jsonInput = JsonSerializer.Serialize(GetAmlInput(srcText, srcLanguage, tgtText, tgtLanguage, traceId, debug, logInput));
                var content = new StringContent(jsonInput);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return client.PostAsync("", content).GetAwaiter().GetResult();
            }
        }
              private static Root GetAmlInput(string? srcText, string? srcLanguage, string? tgtText, string? tgtLanguage, string traceIdGuid, bool debug, bool logInput)
        {
            return new Root()
            {
                source = new Source { language = srcLanguage, text = srcText },
                target = new Target { language = tgtLanguage, text = tgtText },
                options = new Options { debug = debug, logInput = logInput, traceId = traceIdGuid ?? Guid.NewGuid().ToString() },
            };
        }
    }
