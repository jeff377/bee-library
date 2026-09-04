using System.ComponentModel;
using System.Reflection;
using System.Data;
using System.Text.Json;
using Bee.Api.Core.Messages.Form;
using Bee.Api.Core.Messages.System;
using Bee.Api.Core.Transformers;
using Bee.Api.Core.Wire;
using Bee.Definition.Collections;
using Bee.Definition.Filters;
using Bee.Definition.Sorting;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 把 JSON body codec 的編碼規則釘成黃金樣本（`wire-fixtures/bodies/`），
    /// 供另一個語言的 client 對照。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 跨語言的 wire 只有雙向 round-trip 擋得住漂移：.NET 這端有
    /// <c>WireContractDriftTests</c> 守 MessagePack 的註冊，但沒有任何東西看得到
    /// TypeScript 那端。樣本是兩邊唯一的共同事實——.NET 產生並驗證它，TS 用它驗自己
    /// 讀得懂（.NET 寫的）也寫得對（.NET 讀得回）。
    /// </para>
    /// <para>
    /// <b>樣本只固定 body 原文，不固定壓縮或加密後的 bytes。</b>gzip 的輸出跨 .NET
    /// 版本不保證一致，而 AES-CBC 每次用隨機 IV，本質上就不可能固定。這兩層是標準
    /// 演算法、各語言的 library 自己保證；需要釘住的是只有這個框架知道的 JSON 形狀。
    /// </para>
    /// <para>
    /// 要重新產生（**只有在刻意變更編碼規則時**）：
    /// <c>BEE_REGENERATE_WIRE_FIXTURES=1 dotnet test tests/Bee.Api.Core.UnitTests/…</c>
    /// 然後把 diff 讀過一遍再 commit——那份 diff 就是 wire 的變更說明。
    /// </para>
    /// </remarks>
    public class WireFixtureTests
    {
        private static readonly JsonSerializerOptions s_pretty = new() { WriteIndented = true };
        private static readonly Guid s_fixedGuid = new("6f9619ff-8b86-d011-b42d-00c04fc964ff");
        private static readonly DateTime s_fixedUtc = new(2026, 3, 14, 15, 9, 26, 535, DateTimeKind.Utc);

        /// <summary>
        /// 每個樣本涵蓋一條編碼規則，而不是一個訊息型別。
        /// </summary>
        /// <remarks>
        /// 逐訊息型別產樣本會得到上百個幾乎同構的檔案，卻漏掉真正會出錯的地方：
        /// 判別碼、DataTable 形狀、camelCase、列舉的字串化。訊息型別本身是屬性袋，
        /// TS 端由型別定義產生即可。
        /// </remarks>
        private static IEnumerable<(string Name, object Value, Type Type, string Description)> Cases()
        {
            // 1. object 型別成員的判別式封套：JSON 分不出的每一組都要有樣本
            foreach (var (name, value, desc) in ObjectMemberValues())
                yield return ($"value-{name}", new Parameter("v", value), typeof(Parameter), desc);

            // 2. DataTable：型別由 column metadata 還原，不走封套
            yield return ("datatable", BuildTable(), typeof(DataTable),
                "DataTable: cell types are restored from column metadata, so cells carry no discriminator. Covers rowState and the original/current pair on a Modified row.");

            // 3. DataSet：master-detail 與 relations
            yield return ("dataset", BuildDataSet(), typeof(DataSet),
                "DataSet: the shape of tables and relations.");

            // 4. 訊息型別：camelCase、列舉字串化、巢狀集合
            yield return ("message-ping-request", new PingRequest { ClientName = "web", TraceId = "t-001" },
                typeof(PingRequest), "A message type: camelCase naming and the parameters collection inherited from ApiRequest.");

            yield return ("message-getlist-request", BuildGetListRequest(), typeof(GetListRequest),
                "A nested filter tree and sort fields; enums such as the comparison operator travel as strings.");
        }

        private static IEnumerable<(string Name, object Value, string Description)> ObjectMemberValues()
        {
            yield return ("boolean", true, "WireValueCode.Boolean.");
            yield return ("byte", (byte)200, "WireValueCode.Byte.");
            yield return ("sbyte", (sbyte)-100, "WireValueCode.SByte.");
            yield return ("int16", (short)-30000, "WireValueCode.Int16.");
            yield return ("uint16", (ushort)60000, "WireValueCode.UInt16.");
            yield return ("int32", -2000000000, "WireValueCode.Int32.");
            yield return ("uint32", 4000000000u, "WireValueCode.UInt32.");
            yield return ("int64", 9007199254740993L,
                "WireValueCode.Int64. Quoted: a JSON number is a double to every JavaScript reader, which cannot hold an integer past 2^53.");
            yield return ("uint64", 18446744073709551615ul,
                "WireValueCode.UInt64. Quoted for the same reason as Int64.");
            yield return ("single", 1.5f, "WireValueCode.Single.");
            yield return ("double", 1.7976931348623157E+308, "WireValueCode.Double.");
            yield return ("decimal", 79228162514264337593543950335m,
                "WireValueCode.Decimal. Quoted: a JSON number is a double and cannot hold a decimal's precision.");
            yield return ("string", "hello", "WireValueCode.String.");
            yield return ("datetime", s_fixedUtc,
                "WireValueCode.DateTime. Round-trip \"O\" format, which keeps DateTimeKind (ADR-032 depends on it).");
            yield return ("datetimeoffset", new DateTimeOffset(s_fixedUtc).ToOffset(TimeSpan.FromHours(8)),
                "WireValueCode.DateTimeOffset. Round-trip \"O\" format.");
            yield return ("timespan", new TimeSpan(1, 2, 3, 4, 5), "WireValueCode.TimeSpan. Constant \"c\" format.");
            yield return ("dateonly", new DateOnly(2026, 3, 14), "WireValueCode.DateOnly. Round-trip \"O\" format.");
            yield return ("guid", s_fixedGuid, "WireValueCode.Guid. \"D\" format; must not decay into a plain string.");
            yield return ("bytearray", new byte[] { 1, 2, 250, 255 }, "WireValueCode.ByteArray. Base64.");
            yield return ("dbnull", DBNull.Value,
                "WireValueCode.DBNull. The value is written as null; the discriminator is what separates it from a real null.");
            yield return ("objectarray", new object[] { 1, "two", 3.5m },
                "WireValueCode.ObjectArray. Each element carries its own discriminator, recursing through the same envelope.");
            yield return ("datatable", BuildTable(),
                "WireValueCode.DataTable. A DataTable reached through an object-typed member still carries a discriminator - unlike a top-level DataTable body, where the type is already known. See the `datatable` fixture for the payload shape itself.");
            yield return ("null", null!, "A null object-typed member is omitted from the JSON entirely - the property is absent, not written as null. A reader must treat a missing property as null.");
        }

        private static DataTable BuildTable()
        {
            var table = new DataTable("Employee");
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("hired_at", typeof(DateTime));
            table.Columns.Add("row_guid", typeof(Guid));
            table.PrimaryKey = [table.Columns["sys_id"]!];

            var unchanged = table.Rows.Add("E001", 1234.56m, s_fixedUtc, s_fixedGuid);
            var modified = table.Rows.Add("E002", 10m, s_fixedUtc, s_fixedGuid);
            table.AcceptChanges();
            _ = unchanged;
            modified["amount"] = 99.99m;   // 造出 Modified：current 與 original 都要上線

            table.Rows.Add("E003", 7m, s_fixedUtc, s_fixedGuid);  // Added
            return table;
        }

        private static DataSet BuildDataSet()
        {
            var dataSet = new DataSet("Order");
            var master = new DataTable("Master");
            master.Columns.Add("sys_id", typeof(string));
            master.Rows.Add("O001");

            var detail = new DataTable("Detail");
            detail.Columns.Add("sys_id", typeof(string));
            detail.Columns.Add("sys_master_rowid", typeof(string));
            detail.Rows.Add("D001", "O001");

            dataSet.Tables.Add(master);
            dataSet.Tables.Add(detail);
            dataSet.Relations.Add("Master_Detail",
                master.Columns["sys_id"]!, detail.Columns["sys_master_rowid"]!);
            dataSet.AcceptChanges();
            return dataSet;
        }

        private static GetListRequest BuildGetListRequest()
        {
            var group = new FilterGroup(LogicalOperator.And);
            group.Nodes.Add(new FilterCondition("amount", ComparisonOperator.GreaterThan, 100m));
            group.Nodes.Add(new FilterCondition(
                "hired_at", ComparisonOperator.Between, s_fixedUtc, s_fixedUtc.AddDays(30)));

            var sortFields = new SortFieldCollection
            {
                new SortField("sys_id", SortDirection.Desc)
            };

            return new GetListRequest
            {
                SelectFields = "sys_id,amount",
                Filter = group,
                SortFields = sortFields
            };
        }

        #region 樣本檔案

        private static string FixtureDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Bee.Library.slnx")))
                dir = dir.Parent;

            Assert.NotNull(dir);   // 找不到 repo 根就不能默默通過
            return Path.Combine(dir!.FullName, "wire-fixtures", "bodies");
        }

        private static bool RegenerateRequested =>
            Environment.GetEnvironmentVariable("BEE_REGENERATE_WIRE_FIXTURES") == "1";

        /// <summary>
        /// 以現行 codec 序列化案例，取回 body 的 JSON 文字。
        /// </summary>
        private static string EncodeBody(object value, Type type)
        {
            var bytes = new JsonPayloadSerializer().Serialize(value, type);
            return global::System.Text.Encoding.UTF8.GetString(bytes);
        }

        private static string BuildFixtureText(string name, string description, Type type, string body)
        {
            var fixture = new global::System.Text.Json.Nodes.JsonObject
            {
                ["case"] = name,
                ["description"] = description,
                ["codec"] = PayloadCodecNames.Json,
                ["type"] = type.FullName + ", " + type.Assembly.GetName().Name,
                ["body"] = global::System.Text.Json.Nodes.JsonNode.Parse(body)
            };
            return fixture.ToJsonString(s_pretty);
        }

        #endregion

        [Fact]
        [DisplayName("wire 樣本應與現行 JSON codec 的編碼一致（不一致代表 wire 變了）")]
        public void Fixtures_MatchCurrentEncoding()
        {
            var dir = FixtureDirectory();
            if (RegenerateRequested)
                Directory.CreateDirectory(dir);

            var mismatches = new List<string>();

            foreach (var (name, value, type, description) in Cases())
            {
                var body = EncodeBody(value, type);
                var expected = BuildFixtureText(name, description, type, body);
                var path = Path.Combine(dir, name + ".json");

                if (RegenerateRequested)
                {
                    File.WriteAllText(path, expected + global::System.Environment.NewLine);
                    continue;
                }

                if (!File.Exists(path))
                {
                    mismatches.Add($"{name}: 樣本檔不存在（{path}）");
                    continue;
                }

                var actual = File.ReadAllText(path).TrimEnd();
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                    mismatches.Add($"{name}: 樣本與現行編碼不符");
            }

            Assert.True(mismatches.Count == 0,
                "JSON body codec 的編碼與 wire 樣本不符：" + global::System.Environment.NewLine +
                string.Join(global::System.Environment.NewLine, mismatches) + global::System.Environment.NewLine +
                "若這是刻意的 wire 變更，以 BEE_REGENERATE_WIRE_FIXTURES=1 重新產生並逐筆讀過 diff；" +
                "跨語言的 client 會依這份樣本解析，改動即為破壞性變更。");
        }

        [Fact]
        [DisplayName("樣本以 fixture 的 body 反序列化後再序列化應原樣復現（TS 端寫回來時 .NET 讀得回）")]
        public void Fixtures_RoundTripThroughDeserialize()
        {
            var dir = FixtureDirectory();
            if (RegenerateRequested)
                return;   // 重新產生的那一輪還沒有可讀的樣本

            foreach (var (name, _, type, _) in Cases())
            {
                var path = Path.Combine(dir, name + ".json");
                Assert.True(File.Exists(path), $"{name}: 樣本檔不存在（{path}）");

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var body = doc.RootElement.GetProperty("body").GetRawText();

                var serializer = new JsonPayloadSerializer();
                var restored = serializer.Deserialize(global::System.Text.Encoding.UTF8.GetBytes(body), type);
                Assert.NotNull(restored);

                var reencoded = global::System.Text.Encoding.UTF8.GetString(serializer.Serialize(restored!, type));
                using var expectedDoc = JsonDocument.Parse(body);
                using var actualDoc = JsonDocument.Parse(reencoded);
                Assert.Equal(
                    JsonSerializer.Serialize(expectedDoc.RootElement),
                    JsonSerializer.Serialize(actualDoc.RootElement));
            }
        }

        [Fact]
        [DisplayName("樣本集合不得萎縮：判別碼與結構案例都必須在（避免上面兩條檢查變成恆真）")]
        public void FixtureSet_IsNotVacuous()
        {
            var names = Cases().Select(c => c.Name).ToHashSet(StringComparer.Ordinal);

            // 具名 canary 而非數字下限：命名規則一改，數字比對會默默照過。
            string[] required =
            [
                "value-decimal", "value-int64", "value-guid", "value-datetime",
                "value-dbnull", "value-objectarray", "value-null",
                "datatable", "dataset", "message-getlist-request",
            ];
            foreach (var name in required)
                Assert.Contains(name, names);

            // 每個判別碼都要有樣本：漏一個就是 TS 端某個型別會靜默錯值。
            //
            // 這裡刻意由 WireValueCode 的常數反射推導，而不是比對一個數字。原本寫的是
            // `Assert.Equal(22, codeCases.Count)`，那個斷言有兩個問題：新增判別碼而不補樣本時
            // 數量不變、照樣綠；而且它當時**已經是錯的**——22 個 value-* 檔其實是「21 個判別碼
            // + value-null」，WireValueCode.DataTable(21) 從來沒有樣本，數字相等純屬巧合。
            var codes = typeof(WireValueCode)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && f.FieldType == typeof(int) && f.Name != nameof(WireValueCode.Count))
                .Select(f => f.Name)
                .ToList();

            // 防空轉：反射條件寫失準時，下面的 foreach 會一圈都不跑。
            Assert.Equal(WireValueCode.Count - 1, codes.Count);

            foreach (var code in codes)
            {
                string fixtureName = $"value-{code.ToLowerInvariant()}";
                Assert.True(
                    names.Contains(fixtureName),
                    $"WireValueCode.{code} 沒有對應的 {fixtureName} 樣本。跨語言 client 沒有東西可以" +
                    "對照這個判別碼的形狀，寫錯了不會有人發現。");
            }
        }
    }
}
