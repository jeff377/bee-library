using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.DefineAccess;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class RemoteDefineAccessDbTests
    {
        private static (SystemApiConnector connector, RemoteDefineAccess access) CreateAuthenticatedAccess()
        {
            var initConnector = new SystemApiConnector(Guid.NewGuid());
            var token = initConnector.CreateSession("001");
            var connector = new SystemApiConnector(token);
            return (connector, new RemoteDefineAccess(connector));
        }

        [DbFact]
        [DisplayName("RemoteDefineAccess.GetDefine SystemSettings 應回傳有效物件")]
        public void GetDefine_SystemSettings_ReturnsNotNull()
        {
            var (_, access) = CreateAuthenticatedAccess();

            var result = (SystemSettings)access.GetDefine(DefineType.SystemSettings);

            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("RemoteDefineAccess.GetDefine DatabaseSettings 應回傳有效物件")]
        public void GetDefine_DatabaseSettings_ReturnsNotNull()
        {
            var (_, access) = CreateAuthenticatedAccess();

            var result = (DatabaseSettings)access.GetDefine(DefineType.DatabaseSettings);

            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("RemoteDefineAccess.GetDefine DbSchemaSettings 應回傳有效物件")]
        public void GetDefine_DbSchemaSettings_ReturnsNotNull()
        {
            var (_, access) = CreateAuthenticatedAccess();

            var result = (DbSchemaSettings)access.GetDefine(DefineType.DbSchemaSettings);

            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("RemoteDefineAccess.GetDefine TableSchema 帶有效 keys 應回傳有效物件")]
        public void GetDefine_TableSchema_ValidKeys_ReturnsNotNull()
        {
            var (_, access) = CreateAuthenticatedAccess();

            var result = (TableSchema)access.GetDefine(DefineType.TableSchema, new[] { "common", "st_user" });

            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("RemoteDefineAccess.GetDefine FormSchema 帶有效 key 應回傳有效物件")]
        public void GetDefine_FormSchema_ValidKey_ReturnsNotNull()
        {
            var (_, access) = CreateAuthenticatedAccess();

            var result = (FormSchema)access.GetDefine(DefineType.FormSchema, new[] { "Employee" });

            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("RemoteDefineAccess 重複呼叫相同 DefineType 應走快取回傳相同實例")]
        public void GetDefine_SecondCall_ReturnsCachedSameInstance()
        {
            var (_, access) = CreateAuthenticatedAccess();

            var first = (DbSchemaSettings)access.GetDefine(DefineType.DbSchemaSettings);
            var second = (DbSchemaSettings)access.GetDefine(DefineType.DbSchemaSettings);

            Assert.NotNull(first);
            Assert.Same(first, second);
        }
    }
}
