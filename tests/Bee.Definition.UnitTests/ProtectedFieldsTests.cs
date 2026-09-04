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

        /// <summary>
        /// <c>st_user.password</c> 為受保護欄。
        /// </summary>
        /// <remarks>
        /// 這條在 2026-09-04 之前是反過來寫的（`password` 被列在「不受保護」的案例裡），
        /// 等於把一條提權路徑寫成了規格：部署只要在 <c>st_user</c> 上建使用者維護表單，
        /// FormSchema 資料路徑就能寫該欄，把別人的密碼改成驗證器會無條件接受的值。
        /// </remarks>
        [Fact]
        [DisplayName("st_user.password 為受保護欄")]
        public void IsProtected_Password_ReturnsTrue()
        {
            Assert.True(ProtectedFields.IsProtected("st_user", ProtectedFields.Password));
        }

        [Fact]
        [DisplayName("受保護欄的判定不分大小寫（識別碼型比對）")]
        public void IsProtected_IgnoresCase()
        {
            Assert.True(ProtectedFields.IsProtected("ST_USER", "Deployment_Admin"));
        }

        [Theory]
        [InlineData("st_user", "sys_id")]
        [InlineData("st_user", "sys_name")]
        [InlineData("ft_order", "deployment_admin")]
        [InlineData("ft_order", "password")]
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
