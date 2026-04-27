using System.ComponentModel;
using Bee.Definition.Settings;
using Bee.Definition.Database;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// DatabaseServer 資料類別測試。
    /// </summary>
    public class DatabaseServerTests
    {
        [Fact]
        [DisplayName("DatabaseServer 預設值應為空字串與 SQLServer")]
        public void DatabaseServer_Default_HasExpectedDefaults()
        {
            var server = new DatabaseServer();

            Assert.Equal(string.Empty, server.Id);
            Assert.Equal(string.Empty, server.DisplayName);
            Assert.Equal(DatabaseType.SQLServer, server.DatabaseType);
            Assert.Equal(string.Empty, server.ConnectionString);
            Assert.Equal(string.Empty, server.UserId);
            Assert.Equal(string.Empty, server.Password);
        }

        [Fact]
        [DisplayName("DatabaseServer.Id 應對映至 Key")]
        public void DatabaseServer_Id_MapsToKey()
        {
            var server = new DatabaseServer { Id = "main" };

            Assert.Equal("main", server.Key);
            Assert.Equal("main", server.Id);
        }

        [Fact]
        [DisplayName("DatabaseServer.Clone 應產生獨立等值副本")]
        public void DatabaseServer_Clone_ProducesEqualCopy()
        {
            var server = new DatabaseServer
            {
                Id = "main",
                DisplayName = "主資料庫",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=.;Database=Test;",
                UserId = "sa",
                Password = "pw"
            };

            var clone = server.Clone();

            Assert.NotSame(server, clone);
            Assert.Equal(server.Id, clone.Id);
            Assert.Equal(server.DisplayName, clone.DisplayName);
            Assert.Equal(server.DatabaseType, clone.DatabaseType);
            Assert.Equal(server.ConnectionString, clone.ConnectionString);
            Assert.Equal(server.UserId, clone.UserId);
            Assert.Equal(server.Password, clone.Password);
        }

        [Fact]
        [DisplayName("DatabaseServer.ToString 應回傳 \"Id - DisplayName\"")]
        public void DatabaseServer_ToString_ReturnsFormatted()
        {
            var server = new DatabaseServer
            {
                Id = "main",
                DisplayName = "主資料庫"
            };

            Assert.Equal("main - 主資料庫", server.ToString());
        }
    }
}
