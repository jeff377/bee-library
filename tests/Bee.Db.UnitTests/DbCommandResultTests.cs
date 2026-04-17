using System.ComponentModel;
using System.Data;

namespace Bee.Db.UnitTests
{
    public class DbCommandResultTests
    {
        [Fact]
        [DisplayName("ForRowsAffected 應回傳 NonQuery Kind 並設定 RowsAffected")]
        public void ForRowsAffected_SetsKindAndRows()
        {
            var result = DbCommandResult.ForRowsAffected(7);

            Assert.Equal(DbCommandKind.NonQuery, result.Kind);
            Assert.Equal(7, result.RowsAffected);
            Assert.Null(result.Scalar);
            Assert.Null(result.Table);
        }

        [Fact]
        [DisplayName("ForScalar 應回傳 Scalar Kind 並保留 value")]
        public void ForScalar_SetsKindAndScalar()
        {
            var result = DbCommandResult.ForScalar(123);

            Assert.Equal(DbCommandKind.Scalar, result.Kind);
            Assert.Equal(123, result.Scalar);
            Assert.Equal(0, result.RowsAffected);
            Assert.Null(result.Table);
        }

        [Fact]
        [DisplayName("ForScalar value 為 null 仍回傳 Scalar Kind")]
        public void ForScalar_NullValue_ReturnsScalarKind()
        {
            var result = DbCommandResult.ForScalar(null);

            Assert.Equal(DbCommandKind.Scalar, result.Kind);
            Assert.Null(result.Scalar);
        }

        [Fact]
        [DisplayName("ForTable 應回傳 DataTable Kind 並保留 table 參考")]
        public void ForTable_SetsKindAndTable()
        {
            var table = new DataTable("demo");
            var result = DbCommandResult.ForTable(table);

            Assert.Equal(DbCommandKind.DataTable, result.Kind);
            Assert.Same(table, result.Table);
            Assert.Equal(0, result.RowsAffected);
            Assert.Null(result.Scalar);
        }
    }
}
