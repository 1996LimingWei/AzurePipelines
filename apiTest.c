using GenderDebias.Common;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace GenderDebias.Api.Test
{
    [TestClass]
    public class XEGenderDebiasApiTest
    {
        const string TEST_OWNER = "v-limingwei";
        const string XE_API_CATEGORY = "XEGenderDebiasApiTests";
        public TestContext? TestContext { get; set; }
        private string DeploymentName { get; set; } = "";
        [TestInitialize]
        public void TestInitialize()
        {
            var properties = Sanity.RequiresNotNoll(TestContext, $"Test context is null.").Properties;
            string envType = properties.GetValue("GenderDebias_EnvType");
            string endPoint = properties.GetValue($"XE_GenderDebias_Endpoint_{envType}");
            string secretKey = properties.GetValue($"XE_GenderDebias_Key_{envType}");
            DeploymentName = properties.GetValue("azureml-model-deployment");
            ApiConnectHelper.InitApi(envType,endPoint,secretKey);
        }
        #region XE api test

        [TestMethod, Owner(TEST_OWNER), TestCategory(XE_API_CATEGORY)]
        [DynamicData(nameof(OptionFlag))]
        public void SampleXETest(bool debug, bool logInput)
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string srcText = $"dummy src {timeStamp}";
            string tgtText = $"dummy tgt {timeStamp}";
            var response = ApiConnectHelper.CallGdbSvcApi("3", srcText, "es", tgtText, "en", DeploymentName, out _, debug, logInput);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            string responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Deserialize the response content to ApiResponse object
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);
            Assert.IsNotNull(apiResponse, "API response is null");

            // Compare request text and response text
            Assert.AreEqual(srcText, apiResponse.SrcSentence, "Source texts do not match");
            Assert.AreEqual(tgtText, apiResponse.tgt.Neutral, "Target texts do not match");
        }

        #endregion

        #region Test data
        public static IEnumerable<object[]> OptionFlag
        {
            get
            {
                return new[]
                {
                    new object[]{ true, true },
                    new object[]{ true, false },
                    new object[]{ false, true },
                    new object[]{ false, false },
                };
            }
        }
        #endregion

        #region other classes
        public class ApiResponse
        {
            [JsonPropertyName("src_sentence")]
            public string? SrcSentence { get; set; }
            public TranslationResult? tgt { get; set; }
        }

        public class TranslationResult
        {
            public string? Neutral { get; set; }
        }

        #endregion
    }
}
