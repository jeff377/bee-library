using System.ComponentModel;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 把產生的 TypeScript 合約釘住：訊息型別一改，這裡就紅。
    /// </summary>
    /// <remarks>
    /// 與 <c>WireFixtureTests</c> 同一個模式，守的是另一半——樣本釘住「值長什麼樣」，
    /// 這裡釘住「型別有哪些欄位」。兩者都不是為了阻止修改，而是不讓修改在無人察覺下發生：
    /// 欄位改名對舊的跨語言 client 是破壞性變更，而編譯器看不到那一端。
    /// <para>
    /// 要重新產生（**只有在刻意變更合約時**）：
    /// <c>BEE_REGENERATE_WIRE_CONTRACTS=1 dotnet test tests/Bee.Api.Core.UnitTests/…</c>
    /// 然後把 diff 讀過一遍再 commit。
    /// </para>
    /// </remarks>
    public class WireContractGeneratorTests
    {
        private static string ContractPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Bee.Library.slnx")))
                dir = dir.Parent;

            Assert.NotNull(dir);
            return Path.Combine(dir!.FullName, "wire-contracts", "messages.d.ts");
        }

        private static bool RegenerateRequested =>
            Environment.GetEnvironmentVariable("BEE_REGENERATE_WIRE_CONTRACTS") == "1";

        [Fact]
        [DisplayName("產生的 TypeScript 合約應與現行訊息型別一致")]
        public void GeneratedContract_MatchesMessageTypes()
        {
            var generated = WireContractGenerator.Generate();
            var path = ContractPath();

            if (RegenerateRequested)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, generated);
                return;
            }

            Assert.True(File.Exists(path), $"合約檔不存在（{path}）。以 BEE_REGENERATE_WIRE_CONTRACTS=1 產生。");

            var actual = File.ReadAllText(path);
            Assert.True(string.Equals(actual, generated, StringComparison.Ordinal),
                "訊息型別與已產生的 TypeScript 合約不符。" + Environment.NewLine +
                "若這是刻意的合約變更，以 BEE_REGENERATE_WIRE_CONTRACTS=1 重新產生並逐筆讀過 diff——" +
                "欄位改名或移除，對不隨框架一起發版的 client 是破壞性變更。");
        }

        [Fact]
        [DisplayName("產生的內容不得萎縮：關鍵訊息型別與 wire 專屬形狀都必須在")]
        public void GeneratedContract_IsNotVacuous()
        {
            var generated = WireContractGenerator.Generate();

            // 具名 canary 而非數量下限：命名空間搬家會讓產出變空，而數字比對照樣會過。
            string[] required =
            [
                "export interface LoginRequest {",
                "export interface GetListRequest {",
                "export interface PingResponse {",
                // wire 專屬形狀：反射看不出這幾個，它們由自訂 converter 決定。
                "export type WireValueEnvelope =",
                "export interface DataSet {",
                "export interface DataTable {",
            ];
            foreach (var fragment in required)
                Assert.Contains(fragment, generated, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("wire 形狀而非 CLR 形狀：Guid 與 DateTime 應為字串，列舉應為字串字面值聯集")]
        public void GeneratedContract_DescribesWireShapeNotClrShape()
        {
            var generated = WireContractGenerator.Generate();

            // LoginResponse.AccessToken 是 Guid，在 JSON 上是字串。
            Assert.Contains("accessToken: string;", generated, StringComparison.Ordinal);
            // 列舉以字串上線（JsonStringEnumConverter），不是數字。
            Assert.Contains("export type DefineType = '", generated, StringComparison.Ordinal);
            Assert.DoesNotContain(": Guid;", generated, StringComparison.Ordinal);
            Assert.DoesNotContain(": DateTime;", generated, StringComparison.Ordinal);
        }
    }
}
