using System.ComponentModel;
using Bee.Base.Security;
using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.Definition.Database;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// DatabaseSettings DTO 行為 + DatabaseSettingsCryptor 加解密測試。
    /// 加解密職責於 Phase 5 從 DTO 移出到獨立靜態工具類，不再依賴 process-wide 加密金鑰。
    /// </summary>
    public class DatabaseSettingsTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為空集合與預設狀態")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var settings = new DatabaseSettings();

            Assert.NotNull(settings.Servers);
            Assert.NotNull(settings.Items);
            Assert.Empty(settings.Servers!);
            Assert.Empty(settings.Items!);
            Assert.Equal(SerializeState.None, settings.SerializeState);
            Assert.Equal(string.Empty, settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("Servers 於序列化且集合為空時應回傳 null")]
        public void Servers_EmptyDuringSerialize_ReturnsNull()
        {
            var settings = new DatabaseSettings();
            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Null(settings.Servers);
        }

        [Fact]
        [DisplayName("Items 於序列化且集合為空時應回傳 null")]
        public void Items_EmptyDuringSerialize_ReturnsNull()
        {
            var settings = new DatabaseSettings();
            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Null(settings.Items);
        }

        [Fact]
        [DisplayName("SetObjectFilePath 應更新檔案路徑")]
        public void SetObjectFilePath_UpdatesPath()
        {
            var settings = new DatabaseSettings();

            settings.SetObjectFilePath("/tmp/databases.xml");

            Assert.Equal("/tmp/databases.xml", settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("SetSerializeState 應更新自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var settings = new DatabaseSettings();

            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, settings.SerializeState);
        }

        [Fact]
        [DisplayName("Clone 應深層複製 Servers 與 Items")]
        public void Clone_DeepCopiesServersAndItems()
        {
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer
            {
                Id = "S1",
                DisplayName = "主伺服器",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=.;",
                UserId = "sa",
                Password = "p@ss"
            });
            settings.Items!.Add(new DatabaseItem
            {
                Id = "D1",
                DisplayName = "共用",
                DatabaseType = DatabaseType.SQLServer,
                ServerId = "S1",
                ConnectionString = "Server=.;",
                DbName = "common",
                UserId = "sa",
                Password = "p@ss"
            });

            var clone = settings.Clone();

            Assert.NotSame(settings, clone);
            Assert.NotSame(settings.Servers!, clone.Servers!);
            Assert.Single(clone.Servers!);
            Assert.Single(clone.Items!);
            Assert.Equal("S1", clone.Servers![0].Id);
            Assert.Equal("D1", clone.Items![0].Id);
            Assert.NotSame(settings.Servers[0], clone.Servers![0]);
        }

        [Fact]
        [DisplayName("CreateSerializableCopy 應回傳 DatabaseSettings 的 Clone")]
        public void CreateSerializableCopy_ReturnsClone()
        {
            var settings = new DatabaseSettings();
            settings.Items!.Add(new DatabaseItem { Id = "X" });

            var copy = settings.CreateSerializableCopy();

            Assert.IsType<DatabaseSettings>(copy);
            Assert.NotSame(settings, copy);
            Assert.Single(((DatabaseSettings)copy).Items!);
        }

        [Fact]
        [DisplayName("Cryptor.EncryptInPlace 於 key 為空時不應修改 Password")]
        public void Cryptor_EncryptInPlace_NoKey_IsNoOp()
        {
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain" });
            settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "plain" });

            DatabaseSettingsCryptor.EncryptInPlace(settings, Array.Empty<byte>());

            Assert.Equal("plain", settings.Servers![0].Password);
            Assert.Equal("plain", settings.Items![0].Password);
        }

        [Fact]
        [DisplayName("Cryptor.DecryptInPlace 於 key 為空時不應修改 Password")]
        public void Cryptor_DecryptInPlace_NoKey_IsNoOp()
        {
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "enc:xxx" });
            settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "enc:xxx" });

            DatabaseSettingsCryptor.DecryptInPlace(settings, Array.Empty<byte>());

            Assert.Equal("enc:xxx", settings.Servers![0].Password);
            Assert.Equal("enc:xxx", settings.Items![0].Password);
        }

        [Fact]
        [DisplayName("Cryptor.EncryptInPlace 應加密明文 Password 並加上 enc: 前綴")]
        public void Cryptor_EncryptInPlace_EncryptsPlainPassword()
        {
            var key = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain-server" });
            settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "plain-item" });

            DatabaseSettingsCryptor.EncryptInPlace(settings, key);

            Assert.StartsWith("enc:", settings.Servers![0].Password);
            Assert.StartsWith("enc:", settings.Items![0].Password);
            Assert.NotEqual("plain-server", settings.Servers[0].Password);
            Assert.NotEqual("plain-item", settings.Items[0].Password);
        }

        [Fact]
        [DisplayName("Cryptor.EncryptInPlace 不應二次加密已帶 enc: 前綴的 Password")]
        public void Cryptor_EncryptInPlace_AlreadyEncrypted_NotReEncrypted()
        {
            var key = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "enc:fake-base64" });
            settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "enc:fake-base64" });

            DatabaseSettingsCryptor.EncryptInPlace(settings, key);

            Assert.Equal("enc:fake-base64", settings.Servers![0].Password);
            Assert.Equal("enc:fake-base64", settings.Items![0].Password);
        }

        [Fact]
        [DisplayName("Cryptor.Encrypt + Decrypt 應為明文 Password 的往返")]
        public void Cryptor_EncryptThenDecrypt_RoundTripsPassword()
        {
            var key = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "pa$$w0rd-server" });
            settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "pa$$w0rd-item" });

            DatabaseSettingsCryptor.EncryptInPlace(settings, key);
            Assert.StartsWith("enc:", settings.Servers![0].Password);

            DatabaseSettingsCryptor.DecryptInPlace(settings, key);
            Assert.Equal("pa$$w0rd-server", settings.Servers![0].Password);
            Assert.Equal("pa$$w0rd-item", settings.Items![0].Password);
        }

        [Fact]
        [DisplayName("Cryptor.DecryptInPlace 於 Password 為明文時不應修改")]
        public void Cryptor_DecryptInPlace_PlainPassword_LeftUnchanged()
        {
            var key = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain" });
            settings.Items!.Add(new DatabaseItem { Id = "D1", Password = string.Empty });

            DatabaseSettingsCryptor.DecryptInPlace(settings, key);

            Assert.Equal("plain", settings.Servers![0].Password);
            Assert.Equal(string.Empty, settings.Items![0].Password);
        }

        [Fact]
        [DisplayName("CreateSerializableCopy + Cryptor 加密不應污染原始 cache 物件的 Password")]
        public void CreateSerializableCopy_ThenEncrypt_DoesNotMutateOriginalCache()
        {
            // Regression: GetDefineCore must serialize a deep copy so that the encrypt step's
            // in-place mutation does not write ciphertext back to the cached instance.
            var key = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var cached = new DatabaseSettings();
            cached.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain-server" });
            cached.Items!.Add(new DatabaseItem { Id = "D1", Password = "plain-item" });

            // Simulates SystemBusinessObject.GetDefineCore: deep-copy then encrypt the copy.
            var copy = (DatabaseSettings)((ISerializableClone)cached).CreateSerializableCopy();
            DatabaseSettingsCryptor.EncryptInPlace(copy, key);

            Assert.StartsWith("enc:", copy.Servers![0].Password);
            Assert.StartsWith("enc:", copy.Items![0].Password);

            Assert.Equal("plain-server", cached.Servers![0].Password);
            Assert.Equal("plain-item", cached.Items![0].Password);
            Assert.NotSame(cached.Servers[0], copy.Servers[0]);
            Assert.NotSame(cached.Items[0], copy.Items[0]);
        }

        [Fact]
        [DisplayName("Cryptor.DecryptInPlace 於無效 base64 的 enc: Password 應回傳空字串")]
        public void Cryptor_DecryptInPlace_InvalidBase64_ReturnsEmpty()
        {
            var key = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var settings = new DatabaseSettings();
            settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "enc:not-valid-base64!!!" });
            settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "enc:not-valid-base64!!!" });

            DatabaseSettingsCryptor.DecryptInPlace(settings, key);

            Assert.Equal(string.Empty, settings.Servers![0].Password);
            Assert.Equal(string.Empty, settings.Items![0].Password);
        }
    }
}
