using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// 驗證 <see cref="ProtectedFields"/> 的判定範圍——它是提權防線，過寬會擋掉正常欄位，
    /// 過窄則讓部署端自建的維護表單得以寫入特權欄。
    /// </summary>
    public class ProtectedFieldsTests
    {
        [Fact]
        [DisplayName("st_user.deployment_admin 為受保護欄")]
        public void IsProtected_DeploymentAdmin_ReturnsTrue()
        {
            Assert.True(ProtectedFields.IsProtected("st_user", ProtectedFields.DeploymentAdmin));
        }

        [Fact]
        [DisplayName("受保護欄的判定不分大小寫（識別碼型比對）")]
        public void IsProtected_IgnoresCase()
        {
            Assert.True(ProtectedFields.IsProtected("ST_USER", "Deployment_Admin"));
        }

        [Theory]
        [InlineData("st_user", "sys_id")]
        [InlineData("st_user", "password")]
        [InlineData("ft_order", "deployment_admin")]
        [DisplayName("其他欄位與其他資料表的同名欄不受保護")]
        public void IsProtected_OtherColumns_ReturnFalse(string tableName, string fieldName)
        {
            Assert.False(ProtectedFields.IsProtected(tableName, fieldName));
        }

        [Theory]
        [InlineData("", "deployment_admin")]
        [InlineData("st_user", "")]
        [DisplayName("空白表名或欄名應回傳 false 而非擲例外")]
        public void IsProtected_BlankInput_ReturnsFalse(string tableName, string fieldName)
        {
            Assert.False(ProtectedFields.IsProtected(tableName, fieldName));
        }
    }
}
