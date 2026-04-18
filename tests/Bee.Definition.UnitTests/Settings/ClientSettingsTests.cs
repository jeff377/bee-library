using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// ClientSettings 與端點集合相關類別的測試。
    /// </summary>
    public class ClientSettingsTests
    {
        [Fact]
        [DisplayName("EndpointItem 預設建構子應初始化為空字串")]
        public void EndpointItem_DefaultConstructor_InitializesEmpty()
        {
            var item = new EndpointItem();

            Assert.Equal(string.Empty, item.Name);
            Assert.Equal(string.Empty, item.Endpoint);
        }

        [Fact]
        [DisplayName("EndpointItem 帶參數建構子應正確設定 Name 與 Endpoint")]
        public void EndpointItem_ParameterizedConstructor_SetsProperties()
        {
            var item = new EndpointItem("primary", "https://api.example.com");

            Assert.Equal("primary", item.Name);
            Assert.Equal("https://api.example.com", item.Endpoint);
        }

        [Fact]
        [DisplayName("EndpointItemCollection Add(name, endpoint) 應新增並回傳項目")]
        public void EndpointItemCollection_Add_AddsAndReturnsItem()
        {
            var collection = new EndpointItemCollection();

            var item = collection.Add("primary", "https://api.example.com");

            Assert.Single(collection);
            Assert.Same(item, collection[0]);
            Assert.Equal("primary", item.Name);
            Assert.Equal("https://api.example.com", item.Endpoint);
        }

        [Fact]
        [DisplayName("ClientSettings 預設建構子應設定 CreateTime 與空 Endpoint")]
        public void ClientSettings_DefaultConstructor_InitializesState()
        {
            var before = DateTime.Now;
            var settings = new ClientSettings();
            var after = DateTime.Now;

            Assert.InRange(settings.CreateTime, before.AddSeconds(-1), after.AddSeconds(1));
            Assert.Equal(string.Empty, settings.Endpoint);
            Assert.Equal(SerializeState.None, settings.SerializeState);
            Assert.Equal(string.Empty, settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("ClientSettings.EndpointItems 在未序列化狀態下應回傳集合")]
        public void ClientSettings_EndpointItems_ReturnsCollection()
        {
            var settings = new ClientSettings();

            var items = settings.EndpointItems;

            Assert.NotNull(items);
            items!.Add("primary", "https://api.example.com");
            Assert.Single(settings.EndpointItems!);
        }

        [Fact]
        [DisplayName("ClientSettings.Endpoint 可設定與讀取")]
        public void ClientSettings_Endpoint_CanBeSet()
        {
            var settings = new ClientSettings
            {
                Endpoint = "https://api.example.com"
            };

            Assert.Equal("https://api.example.com", settings.Endpoint);
        }

        [Fact]
        [DisplayName("ClientSettings.SetSerializeState 應更新序列化狀態")]
        public void ClientSettings_SetSerializeState_UpdatesState()
        {
            var settings = new ClientSettings();

            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, settings.SerializeState);
        }

        [Fact]
        [DisplayName("ClientSettings.SetObjectFilePath 應更新檔案路徑")]
        public void ClientSettings_SetObjectFilePath_UpdatesPath()
        {
            var settings = new ClientSettings();

            settings.SetObjectFilePath("/tmp/client.xml");

            Assert.Equal("/tmp/client.xml", settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("ClientSettings.EndpointItems 於序列化且空集合時應回傳 null")]
        public void ClientSettings_EndpointItems_EmptyDuringSerialize_ReturnsNull()
        {
            var settings = new ClientSettings();
            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Null(settings.EndpointItems);
        }
    }
}
