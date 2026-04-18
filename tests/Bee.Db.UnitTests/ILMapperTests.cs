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

        [Fact]
        [DisplayName("相同欄位結構第二次呼叫應回傳 cache 中的 mapper")]
        public void CreateMapFunc_SameShape_ReusesCachedDelegate()
        {
            ILMapper<SamplePoco>.ClearCache();

            using var r1 = BuildTable().CreateDataReader();
            r1.Read();
            var first = ILMapper<SamplePoco>.CreateMapFunc(r1);

            using var r2 = BuildTable().CreateDataReader();
            r2.Read();
            var second = ILMapper<SamplePoco>.CreateMapFunc(r2);

            Assert.Same(first, second);
            Assert.Equal(1, ILMapper<SamplePoco>.CacheCount);
        }

        public class AllTypePoco
        {
            public short S16 { get; set; }
            public int I32 { get; set; }
            public long I64 { get; set; }
            public decimal Dec { get; set; }
            public double D { get; set; }
            public float F { get; set; }
            public DateTime Dt { get; set; }
            public int ReadOnlyProp { get; } = 999; // 無 setter,應被略過
        }

        [Fact]
        [DisplayName("各型別欄位應對應至 DbDataReader 對應的 GetXXX 方法")]
        public void CreateMapFunc_AllSupportedTypes_MapsCorrectly()
        {
            ILMapper<AllTypePoco>.ClearCache();

            var table = new DataTable();
            table.Columns.Add("S16", typeof(short));
            table.Columns.Add("I32", typeof(int));
            table.Columns.Add("I64", typeof(long));
            table.Columns.Add("Dec", typeof(decimal));
            table.Columns.Add("D", typeof(double));
            table.Columns.Add("F", typeof(float));
            table.Columns.Add("Dt", typeof(DateTime));
            // ReadOnlyProp 欄位存在,但因屬性無 setter 應被跳過
            table.Columns.Add("ReadOnlyProp", typeof(int));

            var dt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc);
            table.Rows.Add((short)12, 34, 56L, 7.89m, 1.23d, 4.56f, dt, 123);

            using var reader = table.CreateDataReader();
            reader.Read();

            var mapper = ILMapper<AllTypePoco>.CreateMapFunc(reader);
            var result = mapper(reader);

            Assert.Equal((short)12, result.S16);
            Assert.Equal(34, result.I32);
            Assert.Equal(56L, result.I64);
            Assert.Equal(7.89m, result.Dec);
            Assert.Equal(1.23d, result.D);
            Assert.Equal(4.56f, result.F);
            Assert.Equal(dt, result.Dt);
            // ReadOnlyProp 沒 setter,mapper 不會寫入,屬性應保持建構式中設定的預設值
            Assert.Equal(999, result.ReadOnlyProp);
        }

        public class BoolDoublePoco
        {
            public bool Flag { get; set; }
            public double Ratio { get; set; }
        }

        [Fact]
        [DisplayName("bool 與 double 欄位也應能正確對應到 GetBoolean / GetDouble")]
        public void CreateMapFunc_BoolAndDouble_MapCorrectly()
        {
            ILMapper<BoolDoublePoco>.ClearCache();

            var table = new DataTable();
            table.Columns.Add("Flag", typeof(bool));
            table.Columns.Add("Ratio", typeof(double));
            table.Rows.Add(true, 3.14);

            using var reader = table.CreateDataReader();
            reader.Read();

            var mapper = ILMapper<BoolDoublePoco>.CreateMapFunc(reader);
            var result = mapper(reader);

            Assert.True(result.Flag);
            Assert.Equal(3.14, result.Ratio);
        }

        public class ExtraPropertyPoco
        {
            public int Id { get; set; }
            public string Absent { get; set; } = "default-absent";
        }

        [Fact]
        [DisplayName("T 有但 reader 沒有的欄位應被略過,保留屬性預設值")]
        public void CreateMapFunc_PropertyNotInReader_KeepsDefault()
        {
            ILMapper<ExtraPropertyPoco>.ClearCache();

            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(42);

            using var reader = table.CreateDataReader();
            reader.Read();

            var mapper = ILMapper<ExtraPropertyPoco>.CreateMapFunc(reader);
            var result = mapper(reader);

            Assert.Equal(42, result.Id);
            Assert.Equal("default-absent", result.Absent);
        }
    }
}
