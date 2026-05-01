using System.ComponentModel;
using Bee.Base.Security;
using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.Definition.Database;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// DatabaseSettings 單元測試。BeforeSerialize/AfterDeserialize 的加密路徑由
    /// <c>BackendInfo.ConfigEncryptionKey</c> 是否為空決定；預設為空以驗證無副作用。
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
        [DisplayName("BeforeSerialize 於 ConfigEncryptionKey 為空時不應修改 Password")]
        public void BeforeSerialize_NoEncryptionKey_IsNoOp()
        {
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = Array.Empty<byte>();
                var settings = new DatabaseSettings();
                settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain" });
                settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "plain" });

                settings.BeforeSerialize(SerializeFormat.Xml);

                Assert.Equal("plain", settings.Servers![0].Password);
                Assert.Equal("plain", settings.Items![0].Password);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("AfterSerialize 不應拋出例外")]
        public void AfterSerialize_DoesNotThrow()
        {
            var settings = new DatabaseSettings();

            var ex = Record.Exception(() => settings.AfterSerialize(SerializeFormat.Xml));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("AfterDeserialize 於 ConfigEncryptionKey 為空時不應修改 Password")]
        public void AfterDeserialize_NoEncryptionKey_IsNoOp()
        {
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = Array.Empty<byte>();
                var settings = new DatabaseSettings();
                settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "enc:xxx" });
                settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "enc:xxx" });

                settings.AfterDeserialize(SerializeFormat.Xml);

                Assert.Equal("enc:xxx", settings.Servers![0].Password);
                Assert.Equal("enc:xxx", settings.Items![0].Password);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("BeforeSerialize 於 ConfigEncryptionKey 有值時應加密明文 Password 並加上 enc: 前綴")]
        public void BeforeSerialize_WithEncryptionKey_EncryptsPlainPassword()
        {
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
                var settings = new DatabaseSettings();
                settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain-server" });
                settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "plain-item" });

                settings.BeforeSerialize(SerializeFormat.Xml);

                Assert.StartsWith("enc:", settings.Servers![0].Password);
                Assert.StartsWith("enc:", settings.Items![0].Password);
                Assert.NotEqual("plain-server", settings.Servers[0].Password);
                Assert.NotEqual("plain-item", settings.Items[0].Password);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("BeforeSerialize 不應二次加密已帶 enc: 前綴的 Password")]
        public void BeforeSerialize_AlreadyEncrypted_NotReEncrypted()
        {
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
                var settings = new DatabaseSettings();
                settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "enc:fake-base64" });
                settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "enc:fake-base64" });

                settings.BeforeSerialize(SerializeFormat.Xml);

                Assert.Equal("enc:fake-base64", settings.Servers![0].Password);
                Assert.Equal("enc:fake-base64", settings.Items![0].Password);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("BeforeSerialize + AfterDeserialize 應為明文 Password 的往返")]
        public void BeforeSerializeThenAfterDeserialize_RoundTripsPassword()
        {
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
                var settings = new DatabaseSettings();
                settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "pa$$w0rd-server" });
                settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "pa$$w0rd-item" });

                settings.BeforeSerialize(SerializeFormat.Xml);
                Assert.StartsWith("enc:", settings.Servers![0].Password);

                settings.AfterDeserialize(SerializeFormat.Xml);
                Assert.Equal("pa$$w0rd-server", settings.Servers![0].Password);
                Assert.Equal("pa$$w0rd-item", settings.Items![0].Password);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("AfterDeserialize 於 Password 為明文時不應修改")]
        public void AfterDeserialize_PlainPassword_LeftUnchanged()
        {
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
                var settings = new DatabaseSettings();
                settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain" });
                settings.Items!.Add(new DatabaseItem { Id = "D1", Password = string.Empty });

                settings.AfterDeserialize(SerializeFormat.Xml);

                Assert.Equal("plain", settings.Servers![0].Password);
                Assert.Equal(string.Empty, settings.Items![0].Password);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("CreateSerializableCopy + BeforeSerialize 不應污染原始 cache 物件的 Password")]
        public void CreateSerializableCopy_BeforeSerialize_DoesNotMutateOriginalCache()
        {
            // Regression: GetDefineCore must serialize a deep copy so that BeforeSerialize's
            // in-place encryption does not write ciphertext back to the cached instance.
            // Removing CreateSerializableCopy() from the pipeline would re-introduce the bug.
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
                var cached = new DatabaseSettings();
                cached.Servers!.Add(new DatabaseServer { Id = "S1", Password = "plain-server" });
                cached.Items!.Add(new DatabaseItem { Id = "D1", Password = "plain-item" });

                // Simulates SystemBusinessObject.GetDefineCore: deep-copy then serialize-encrypt the copy.
                var copy = (DatabaseSettings)((ISerializableClone)cached).CreateSerializableCopy();
                copy.BeforeSerialize(SerializeFormat.Xml);

                Assert.StartsWith("enc:", copy.Servers![0].Password);
                Assert.StartsWith("enc:", copy.Items![0].Password);

                Assert.Equal("plain-server", cached.Servers![0].Password);
                Assert.Equal("plain-item", cached.Items![0].Password);
                Assert.NotSame(cached.Servers[0], copy.Servers[0]);
                Assert.NotSame(cached.Items[0], copy.Items[0]);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("AfterDeserialize 於無效 base64 的 enc: Password 應回傳空字串")]
        public void AfterDeserialize_InvalidBase64_ReturnsEmpty()
        {
            var originalKey = BackendInfo.ConfigEncryptionKey;
            try
            {
                BackendInfo.ConfigEncryptionKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
                var settings = new DatabaseSettings();
                settings.Servers!.Add(new DatabaseServer { Id = "S1", Password = "enc:not-valid-base64!!!" });
                settings.Items!.Add(new DatabaseItem { Id = "D1", Password = "enc:not-valid-base64!!!" });

                settings.AfterDeserialize(SerializeFormat.Xml);

                Assert.Equal(string.Empty, settings.Servers![0].Password);
                Assert.Equal(string.Empty, settings.Items![0].Password);
            }
            finally
            {
                BackendInfo.ConfigEncryptionKey = originalKey;
            }
        }
    }
}
