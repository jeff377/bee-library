using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class MemberPathTests
    {
        [Fact]
        [DisplayName("MemberPath.Of 應回傳正確的成員路徑字串")]
        public void Of_StaticProperty_ReturnsFullMemberPath()
        {
            // Act
            var path = MemberPath.Of(() => SysInfo.Version);

            // Assert
            Assert.Equal("SysInfo.Version", path);
        }
    }
}
