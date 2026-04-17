using System.ComponentModel;
using System.Data;

namespace Bee.Db.UnitTests
{
    public class ILMapperTests
    {
        public class SamplePoco
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool Active { get; set; }
        }

        public class NoCtorPoco
        {
            public int Id { get; }

            public NoCtorPoco(int id)
            {
                Id = id;
            }
        }

        private static DataTable BuildTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Active", typeof(bool));
            table.Rows.Add(1, "Alice", true);
            table.Rows.Add(2, "Bob", false);
            return table;
        }

        [Fact]
        [DisplayName("CreateMapFunc 應依 reader 欄位產生對應 mapper")]
        public void CreateMapFunc_MapsAllMatchingFields()
        {
            ILMapper<SamplePoco>.ClearCache();
            using var reader = BuildTable().CreateDataReader();
            reader.Read();

            var mapper = ILMapper<SamplePoco>.CreateMapFunc(reader);
            var result = mapper(reader);

            Assert.Equal(1, result.Id);
            Assert.Equal("Alice", result.Name);
            Assert.True(result.Active);
        }

        [Fact]
        [DisplayName("欄位名稱應採大小寫不敏感比對")]
        public void CreateMapFunc_CaseInsensitiveFieldMatching()
        {
            ILMapper<SamplePoco>.ClearCache();
            var table = new DataTable();
            table.Columns.Add("ID", typeof(int));   // 大小寫不同
            table.Columns.Add("name", typeof(string));
            table.Rows.Add(99, "Carol");

            using var reader = table.CreateDataReader();
            reader.Read();

            var mapper = ILMapper<SamplePoco>.CreateMapFunc(reader);
            var result = mapper(reader);

            Assert.Equal(99, result.Id);
            Assert.Equal("Carol", result.Name);
        }

        [Fact]
        [DisplayName("DBNull 欄位應保留屬性預設值")]
        public void CreateMapFunc_DBNullField_KeepsDefaultValue()
        {
            ILMapper<SamplePoco>.ClearCache();
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(1, DBNull.Value);

            using var reader = table.CreateDataReader();
            reader.Read();

            var mapper = ILMapper<SamplePoco>.CreateMapFunc(reader);
            var result = mapper(reader);

            Assert.Equal(1, result.Id);
            Assert.Equal(string.Empty, result.Name);  // 屬性初始值
        }

        [Fact]
        [DisplayName("MapToList 應將所有 row 對映到 List<T>")]
        public void MapToList_MapsAllRows()
        {
            ILMapper<SamplePoco>.ClearCache();
            using var reader = BuildTable().CreateDataReader();
            // 取得 mapper 前需要 Read() 才能取 schema；改用先建構 mapper 再丟進 MapToList
            // 由於 BuildTable 後又重建 reader 不切實際，使用獨立的 reader 取 mapper
            using var schemaReader = BuildTable().CreateDataReader();
            schemaReader.Read();
            var mapper = ILMapper<SamplePoco>.CreateMapFunc(schemaReader);

            var list = ILMapper<SamplePoco>.MapToList(reader, mapper);

            Assert.Equal(2, list.Count);
            Assert.Equal("Alice", list[0].Name);
            Assert.Equal("Bob", list[1].Name);
        }

        [Fact]
        [DisplayName("MapToEnumerable 應與 MapToList 結果一致")]
        public void MapToEnumerable_MatchesMapToList()
        {
            ILMapper<SamplePoco>.ClearCache();
            using var schemaReader = BuildTable().CreateDataReader();
            schemaReader.Read();
            var mapper = ILMapper<SamplePoco>.CreateMapFunc(schemaReader);

            using var reader = BuildTable().CreateDataReader();
            var enumerated = ILMapper<SamplePoco>.MapToEnumerable(reader, mapper).ToList();

            Assert.Equal(2, enumerated.Count);
            Assert.Equal(1, enumerated[0].Id);
            Assert.Equal(2, enumerated[1].Id);
        }

        [Fact]
        [DisplayName("ClearCache 應清空指定型別的 cache")]
        public void ClearCache_RemovesEntriesForType()
        {
            ILMapper<SamplePoco>.ClearCache();
            using var reader = BuildTable().CreateDataReader();
            reader.Read();
            ILMapper<SamplePoco>.CreateMapFunc(reader);

            Assert.True(ILMapper<SamplePoco>.CacheCount > 0);

            ILMapper<SamplePoco>.ClearCache();

            Assert.Equal(0, ILMapper<SamplePoco>.CacheCount);
        }

        [Fact]
        [DisplayName("不具無參數建構子的型別應擲 InvalidOperationException")]
        public void CreateMapFunc_NoParameterlessCtor_Throws()
        {
            ILMapper<NoCtorPoco>.ClearCache();
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(1);
            using var reader = table.CreateDataReader();
            reader.Read();

            Assert.Throws<InvalidOperationException>(() => ILMapper<NoCtorPoco>.CreateMapFunc(reader));
        }
    }
}
