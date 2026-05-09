using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// DbCategory 單元測試。
    /// </summary>
    public class DbCategoryTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為空字串")]
        public void DefaultConstructor_InitializesEmpty()
        {
            var category = new DbCategory();

            Assert.Equal(string.Empty, category.Id);
            Assert.Equal(string.Empty, category.DisplayName);
        }

        [Fact]
        [DisplayName("Id 應與 Key 對映")]
        public void Id_MapsToKey()
        {
            var category = new DbCategory { Id = "common" };

            Assert.Equal("common", category.Key);

            category.Key = "system";
            Assert.Equal("system", category.Id);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"Id - DisplayName\"")]
        public void ToString_ReturnsFormatted()
        {
            var category = new DbCategory { Id = "common", DisplayName = "共用資料庫" };

            Assert.Equal("common - 共用資料庫", category.ToString());
        }

        [Fact]
        [DisplayName("Tables 未序列化狀態應回傳集合實例")]
        public void Tables_DefaultState_ReturnsCollection()
        {
            var category = new DbCategory();

            Assert.NotNull(category.Tables);
        }

        [Fact]
        [DisplayName("Tables 於序列化且集合為空時應回傳 null")]
        public void Tables_EmptyDuringSerialize_ReturnsNull()
        {
            var category = new DbCategory();
            category.SetSerializeState(SerializeState.Serialize);

            Assert.Null(category.Tables);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var category = new DbCategory();

            category.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, category.SerializeState);
        }
    }
}
