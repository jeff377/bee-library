using System.ComponentModel;

namespace Bee.Db.UnitTests
{
    public class DbAccessFactoryTests
    {
        [Fact]
        [DisplayName("DbAccessFactory 預設建構子應成功建立實例")]
        public void Constructor_Default_Succeeds()
        {
            var exception = Record.Exception(() => new DbAccessFactory());
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("DbAccessFactory 指定 maxCommandTimeout 應成功建立實例")]
        public void Constructor_WithMaxCommandTimeout_Succeeds()
        {
            var exception = Record.Exception(() => new DbAccessFactory(30));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("DbAccessFactory.Create 使用不存在的 databaseId 應拋出 InvalidOperationException")]
        public void Create_UnknownDatabaseId_ThrowsInvalidOperationException()
        {
            var factory = new DbAccessFactory();
            Assert.Throws<InvalidOperationException>(() => factory.Create("nonexistent_db_id_xyz"));
        }
    }
}
