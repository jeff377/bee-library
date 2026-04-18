using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// DbSchema 單元測試。
    /// </summary>
    public class DbSchemaTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為空字串")]
        public void DefaultConstructor_InitializesEmpty()
        {
            var schema = new DbSchema();

            Assert.Equal(string.Empty, schema.DbName);
            Assert.Equal(string.Empty, schema.DisplayName);
        }

        [Fact]
        [DisplayName("DbName 應與 Key 對映")]
        public void DbName_MapsToKey()
        {
            var schema = new DbSchema { DbName = "common" };

            Assert.Equal("common", schema.Key);

            schema.Key = "system";
            Assert.Equal("system", schema.DbName);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"DbName - DisplayName\"")]
        public void ToString_ReturnsFormatted()
        {
            var schema = new DbSchema { DbName = "common", DisplayName = "共用資料庫" };

            Assert.Equal("common - 共用資料庫", schema.ToString());
        }

        [Fact]
        [DisplayName("Tables 未序列化狀態應回傳集合實例")]
        public void Tables_DefaultState_ReturnsCollection()
        {
            var schema = new DbSchema();

            Assert.NotNull(schema.Tables);
        }

        [Fact]
        [DisplayName("Tables 於序列化且集合為空時應回傳 null")]
        public void Tables_EmptyDuringSerialize_ReturnsNull()
        {
            var schema = new DbSchema();
            schema.SetSerializeState(SerializeState.Serialize);

            Assert.Null(schema.Tables);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var schema = new DbSchema();

            schema.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, schema.SerializeState);
        }
    }
}
