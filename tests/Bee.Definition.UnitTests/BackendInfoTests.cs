using System.ComponentModel;
using Bee.Base.Security;
using Bee.Definition.Security;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// <see cref="BackendInfo.Initialize(BackendConfiguration)"/> / <see cref="BackendInfo.Initialize(BackendConfiguration, bool)"/>
    /// 以及 InitializeSecurityKeys 四條分支的測試。
    /// 為避免干擾其他測試共用的全域狀態，測試會在 finally 中還原金鑰屬性。
    /// </summary>
    [Collection("Initialize")]
    public class BackendInfoTests
    {
        private static string WriteTempMasterKey(byte[] masterKey)
        {
            var path = Path.Combine(Path.GetTempPath(), $"bee-backendinfo-mk-{Guid.NewGuid():N}.key");
            File.WriteAllText(path, Convert.ToBase64String(masterKey));
            return path;
        }

        [Fact]
        [DisplayName("Initialize(BackendConfiguration) 單參數 overload 應等同於 autoCreateMasterKey=false")]
        public void Initialize_SingleArg_DelegatesWithAutoCreateFalse()
        {
            byte[] masterKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var keyFile = WriteTempMasterKey(masterKey);

            var savedApi = BackendInfo.ApiEncryptionKey;
            var savedCookie = BackendInfo.CookieEncryptionKey;
            var savedConfig = BackendInfo.ConfigEncryptionKey;
            var savedDb = BackendInfo.DatabaseEncryptionKey;

            try
            {
                var config = new BackendConfiguration();
                config.SecurityKeySettings.MasterKeySource = new MasterKeySource
                {
                    Type = MasterKeySourceType.File,
                    Value = keyFile
                };

                // 單參數 overload 應正常完成；SecurityKeySettings 全為空字串，四個金鑰分支都不會進入。
                BackendInfo.Initialize(config);

                // autoCreateMasterKey=false 時，若給的是不存在的檔案路徑，應拋 FileNotFoundException。
                var missing = Path.Combine(Path.GetTempPath(), $"bee-missing-{Guid.NewGuid():N}.key");
                var badConfig = new BackendConfiguration();
                badConfig.SecurityKeySettings.MasterKeySource = new MasterKeySource
                {
                    Type = MasterKeySourceType.File,
                    Value = missing
                };
                Assert.Throws<FileNotFoundException>(() => BackendInfo.Initialize(badConfig));
            }
            finally
            {
                BackendInfo.ApiEncryptionKey = savedApi;
                BackendInfo.CookieEncryptionKey = savedCookie;
                BackendInfo.ConfigEncryptionKey = savedConfig;
                BackendInfo.DatabaseEncryptionKey = savedDb;
                if (File.Exists(keyFile)) File.Delete(keyFile);
            }
        }

        [Fact]
        [DisplayName("Initialize 應解密所有四組 SecurityKeySettings 加密金鑰到對應 BackendInfo 屬性")]
        public void Initialize_DecryptsAllFourEncryptedKeys()
        {
            byte[] masterKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var keyFile = WriteTempMasterKey(masterKey);

            string apiEnc = EncryptionKeyProtector.GenerateEncryptedKey(masterKey);
            string cookieEnc = EncryptionKeyProtector.GenerateEncryptedKey(masterKey);
            string configEnc = EncryptionKeyProtector.GenerateEncryptedKey(masterKey);
            string dbEnc = EncryptionKeyProtector.GenerateEncryptedKey(masterKey);

            var savedApi = BackendInfo.ApiEncryptionKey;
            var savedCookie = BackendInfo.CookieEncryptionKey;
            var savedConfig = BackendInfo.ConfigEncryptionKey;
            var savedDb = BackendInfo.DatabaseEncryptionKey;

            try
            {
                var config = new BackendConfiguration
                {
                    SecurityKeySettings = new SecurityKeySettings
                    {
                        MasterKeySource = new MasterKeySource
                        {
                            Type = MasterKeySourceType.File,
                            Value = keyFile
                        },
                        ApiEncryptionKey = apiEnc,
                        CookieEncryptionKey = cookieEnc,
                        ConfigEncryptionKey = configEnc,
                        DatabaseEncryptionKey = dbEnc
                    }
                };

                BackendInfo.Initialize(config, autoCreateMasterKey: false);

                // 預期：四組屬性都被解密填入，對應的 byte[] 與 masterKey 解密結果相等。
                var expectedApi = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, apiEnc);
                var expectedCookie = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, cookieEnc);
                var expectedConfig = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, configEnc);
                var expectedDb = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, dbEnc);

                Assert.Equal(expectedApi, BackendInfo.ApiEncryptionKey);
                Assert.Equal(expectedCookie, BackendInfo.CookieEncryptionKey);
                Assert.Equal(expectedConfig, BackendInfo.ConfigEncryptionKey);
                Assert.Equal(expectedDb, BackendInfo.DatabaseEncryptionKey);
            }
            finally
            {
                BackendInfo.ApiEncryptionKey = savedApi;
                BackendInfo.CookieEncryptionKey = savedCookie;
                BackendInfo.ConfigEncryptionKey = savedConfig;
                BackendInfo.DatabaseEncryptionKey = savedDb;
                if (File.Exists(keyFile)) File.Delete(keyFile);
            }
        }

        [Fact]
        [DisplayName("LoginAttemptTracker 可被設為 null 與還原非 null 實例")]
        public void LoginAttemptTracker_SetGet_Roundtrip()
        {
            var original = BackendInfo.LoginAttemptTracker;
            try
            {
                BackendInfo.LoginAttemptTracker = null;
                Assert.Null(BackendInfo.LoginAttemptTracker);

                var tracker = new TestTracker();
                BackendInfo.LoginAttemptTracker = tracker;
                Assert.Same(tracker, BackendInfo.LoginAttemptTracker);
            }
            finally
            {
                BackendInfo.LoginAttemptTracker = original;
            }
        }

        private sealed class TestTracker : ILoginAttemptTracker
        {
            public bool IsLockedOut(string userId) => false;
            public void RecordFailure(string userId) { }
            public void Reset(string userId) { }
        }
    }
}
