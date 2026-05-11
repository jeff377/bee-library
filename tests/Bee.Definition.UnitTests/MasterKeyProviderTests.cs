using Bee.Base.Security;
using Bee.Definition.Security;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    public class MasterKeyProviderTests
    {
        [Fact]
        [DisplayName("GetMasterKey 從環境變數讀取有效金鑰應回傳解碼後的位元組陣列")]
        public void GetMasterKey_EnvironmentSource_ValidVar_ReturnsMasterKey()
        {
            string varName = "BEE_TEST_MASTER_KEY_" + Guid.NewGuid().ToString("N");
            byte[] expectedKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            string base64Key = Convert.ToBase64String(expectedKey);
            Environment.SetEnvironmentVariable(varName, base64Key);
            try
            {
                var source = new MasterKeySource { Type = MasterKeySourceType.Environment, Value = varName };
                byte[] result = MasterKeyProvider.GetMasterKey(source);
                Assert.Equal(expectedKey, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 從環境變數讀取未設定的變數名稱應擲 InvalidOperationException")]
        public void GetMasterKey_EnvironmentSource_MissingVar_ThrowsInvalidOperationException()
        {
            string varName = "BEE_TEST_MASTER_KEY_MISSING_" + Guid.NewGuid().ToString("N");
            Environment.SetEnvironmentVariable(varName, null);
            var source = new MasterKeySource { Type = MasterKeySourceType.Environment, Value = varName };

            Assert.Throws<InvalidOperationException>(() => MasterKeyProvider.GetMasterKey(source));
        }

        [Fact]
        [DisplayName("GetMasterKey 從環境變數讀取非 Base64 內容應擲 InvalidOperationException")]
        public void GetMasterKey_EnvironmentSource_InvalidBase64_ThrowsInvalidOperationException()
        {
            string varName = "BEE_TEST_MASTER_KEY_INVALID_" + Guid.NewGuid().ToString("N");
            Environment.SetEnvironmentVariable(varName, "not-valid-base64!!!");
            try
            {
                var source = new MasterKeySource { Type = MasterKeySourceType.Environment, Value = varName };
                Assert.Throws<InvalidOperationException>(() => MasterKeyProvider.GetMasterKey(source));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 使用空變數名稱時應改用預設名稱 BEE_MASTER_KEY")]
        public void GetMasterKey_EnvironmentSource_EmptyVarName_UsesDefaultName()
        {
            const string defaultVarName = "BEE_MASTER_KEY";
            string? originalValue = Environment.GetEnvironmentVariable(defaultVarName);

            byte[] expectedKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            string base64Key = Convert.ToBase64String(expectedKey);
            Environment.SetEnvironmentVariable(defaultVarName, base64Key);
            try
            {
                var source = new MasterKeySource { Type = MasterKeySourceType.Environment, Value = string.Empty };
                byte[] result = MasterKeyProvider.GetMasterKey(source);
                Assert.Equal(expectedKey, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable(defaultVarName, originalValue);
            }
        }
    }
}
