using GenderDebias.Common;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace GenderDebias.Api.Test
{
    [TestClass]
    public class GenderDebiasApiTest
    {
        const string TEST_OWNER = "v-xianta";
        const string API_CATEGORY = "GenderDebiasApiTests";
        public TestContext? TestContext { get; set; }
        [TestInitialize]
        public void TestInitialize()
        {
            var properties = Sanity.RequiresNotNoll(TestContext, $"Test context is null.").Properties;
            string envType = properties.GetValue("GenderDebias_EnvType");
            string endPoint=properties.GetValue($"GenderDebias_Endpoint_{envType}");
            string secretKey= properties.GetValue($"GenderDebias_Key_{envType}");
            ApiConnectHelper.Init(envType,endPoint,secretKey);
        }
        private void TestLanguageCode(string? invalidCode, bool isSrc, bool succeed)
        {
            var response = isSrc
            ? ApiConnectHelper.CallGdbSvcApi("3", "dummy src", invalidCode, "dummy tgt", "en")
            : ApiConnectHelper.CallGdbSvcApi("3", "dummy src", "en", "dummy tgt", invalidCode);

            var expected = succeed
                ? HttpStatusCode.OK
                : HttpStatusCode.FailedDependency;

            Assert.AreEqual(expected, response.StatusCode);
        }
        #region Test data
        public static IEnumerable<object[]> ValidLanguageCode
        {
            get
            {
                return new[]
                {
                    new object[]{"fr"},
                    new object[]{"it"},
                    new object[]{"es"},
                };
            }
        }
        public static IEnumerable<object[]> NoGenderLanguageCode
        {
            get
            {
                return new[]
                {
                    new object[]{"de"},
                    new object[]{"zh-chs"},
                    new object[]{"zh"},
                    new object[]{"af"},
                    new object[]{"ca"},
                };
            }
        }
        public static IEnumerable<object[]> InvalidLanguageCode
        {
            get
            {
                return new[]
                {
                    new object[]{"dummy"},
                    new object[]{"xyz"},
                    new object[]{"abc"},
                    new object[]{"not a code"},
                };
            }
        }

        public static IEnumerable<object[]> EmptyOrWhiteSpaceString
        {
            get
            {
                return new[]
                {
                    //new object[]{null},
                    new object[]{""},
                    new object[]{" "},
                    new object[]{"\t"},
                    new object[]{" \t"},
                    new object[]{"\t "},
                    new object[]{"\r"},
                    new object[]{"\n"},
                    new object[]{"\r\n"},
                };
            }
        }
        public static IEnumerable<object[]> SpecialString
        {
            get
            {
                return new[]
                {
                    new object[]{new string('\0', 1)},
                    new object[]{new string('a', 65536)},
                    new object[]{new string('\0', 65536)},
                    new object[]{"一二三四五龟"},
                    new object[]{new string('一', 65536)},
                    new object[]{new string('龟', 65536)},
                    new object[]{new string(Enumerable.Range(0, 127).Select(x => (char)x).ToArray())},
                };
            }
        }
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
 #region Language code
        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(ValidLanguageCode))]
        public void ValidLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, false, true);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(NoGenderLanguageCode))]
        public void NoGenderSrcLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, true, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(NoGenderLanguageCode))]
        public void NoGenderTgtLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, false, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(InvalidLanguageCode))]
        public void InvalidSrcLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, true, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(InvalidLanguageCode))]
        public void InvalidTgtLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, false, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(EmptyOrWhiteSpaceString))]
        public void EmptyOrWhiteSpaceSrcLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, true, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        public void NullSrcLanguageCodeTest()
        {
            TestLanguageCode(null, true, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(EmptyOrWhiteSpaceString))]
        public void EmptyOrWhiteSpaceTgtLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, false, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        public void NullTgtLanguageCodeTest()
        {
            TestLanguageCode(null, false, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(SpecialString))]
        public void SpecialSrcLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, true, false);
        }
        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(SpecialString))]
        public void SpecialTgtLanguageCodeTest(string languageCode)
        {
            TestLanguageCode(languageCode, true, false);
        }
        #endregion
        #region Private helper functions


        private void TestContent(string? content, bool isSrc, bool succeed)
        {
            var responseTgt = isSrc
                ? ApiConnectHelper.CallGdbSvcApi("3", content, "en", "dummy tgt", "es")
                : ApiConnectHelper.CallGdbSvcApi("3", "dummy src", "en", content, "es");
            if (succeed)
                Assert.AreEqual(HttpStatusCode.OK, responseTgt.StatusCode);
            else
                Assert.AreEqual(HttpStatusCode.FailedDependency, responseTgt.StatusCode);
        }
        #endregion
#region Content

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(EmptyOrWhiteSpaceString))]
        public void EmptyOrWhiteSpaceSrcContentTest(string content)
        {
            TestContent(content, true, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        public void NullSrcContentTest()
        {
            TestContent(null, true, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(EmptyOrWhiteSpaceString))]
        public void EmptyOrWhiteSpaceTgtContentTest(string content)
        {
            TestContent(content, false, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        public void NullTgtContentTest()
        {
            TestContent(null, false, false);
        }

        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(SpecialString))]
        public void SpecialSrcContentTest(string content)
        {
            TestContent(content, true, true);
        }
        [TestMethod, Owner(TEST_OWNER), TestCategory(API_CATEGORY)]
        [DynamicData(nameof(SpecialString))]
        public void SpecialTgtContentTest(string content)
        {
            TestContent(content, false, true);
        }

        #endregion
