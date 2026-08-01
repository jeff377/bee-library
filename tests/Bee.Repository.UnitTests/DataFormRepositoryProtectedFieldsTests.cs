using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Repository.Form;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 驗證 <c>DataFormRepository.RemoveProtectedFields</c>：即使部署端的 FormSchema 宣告了
    /// 受保護欄，寫入命令的欄位集合也不得包含它。
    /// </summary>
    /// <remarks>
    /// 這是提權防線，不是整理工作——`st_user` 沒有出貨的 FormSchema，但部署端可以自建一張
    /// 使用者維護表單。少了這道剔除，該表單就是一條讓一般使用者把自己標成部署層管理員的路。
    ///
    /// 以 reflection 直接測私有靜態方法，與本測試類既有的 `ConvertDefaultValue` /
    /// `TryCoerceToGuid` 同一做法：防線的行為只在這個方法裡，走完整 Save 需要實體資料表，
    /// 換來的覆蓋卻是同一件事。
    /// </remarks>
    public class DataFormRepositoryProtectedFieldsTests
    {
        private static readonly Type[] s_removeProtectedFieldsParams = [typeof(TableSchema)];

        private static void RemoveProtectedFields(TableSchema tableSchema)
        {
            var method = typeof(DataFormRepository).GetMethod(
                "RemoveProtectedFields", BindingFlags.NonPublic | BindingFlags.Static,
                null, s_removeProtectedFieldsParams, null);
            Assert.NotNull(method);
            method!.Invoke(null, [tableSchema]);
        }

        private static TableSchema BuildUserTableSchema()
        {
            var schema = new FormSchema("st_user", "User");
            var master = schema.Tables!.Add("st_user", "User");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add(SysFields.Id, "User Id", FieldDbType.String);
            master.Fields.Add(ProtectedFields.DeploymentAdmin, "Deployment admin", FieldDbType.Boolean);
            return master.GenerateDbTable();
        }

        [Fact]
        [DisplayName("FormSchema 宣告 st_user.deployment_admin 時，寫入用的 schema 仍不含該欄")]
        public void RemoveProtectedFields_DropsDeploymentAdmin()
        {
            var tableSchema = BuildUserTableSchema();
            Assert.True(tableSchema.Fields!.Contains(ProtectedFields.DeploymentAdmin));

            RemoveProtectedFields(tableSchema);

            Assert.False(tableSchema.Fields.Contains(ProtectedFields.DeploymentAdmin));
        }

        [Fact]
        [DisplayName("剔除受保護欄不影響同表的其他欄位")]
        public void RemoveProtectedFields_KeepsOtherColumns()
        {
            var tableSchema = BuildUserTableSchema();

            RemoveProtectedFields(tableSchema);

            Assert.True(tableSchema.Fields!.Contains(SysFields.RowId));
            Assert.True(tableSchema.Fields.Contains(SysFields.Id));
        }

        [Fact]
        [DisplayName("其他資料表的同名欄不受影響——保護是 table + column 成對判定")]
        public void RemoveProtectedFields_OtherTableSameColumn_Kept()
        {
            var schema = new FormSchema("ft_order", "Order");
            var master = schema.Tables!.Add("ft_order", "Order");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add(ProtectedFields.DeploymentAdmin, "Unrelated column", FieldDbType.Boolean);
            var tableSchema = master.GenerateDbTable();

            RemoveProtectedFields(tableSchema);

            Assert.True(tableSchema.Fields!.Contains(ProtectedFields.DeploymentAdmin));
        }
    }
}
