using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace Bee.Api.AspNetCore.UnitTests
{
    /// <summary>
    /// 把架構文件裡的四條硬約束變成可執行檢查。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這四條是本框架分層宣稱的核心（見 <c>docs/development-constraints.md</c> 與
    /// <c>docs/dependency-map.md</c>），先前<b>只靠每輪體檢時人／代理重掃</b>來確認 ——
    /// 而 ADR-038 那一條邊反而有兩道閘門守著。體檢一年跑幾次，違規進 main 到被發現之間
    /// 是好幾週。
    /// </para>
    /// <para>
    /// <b>為什麼放在這個測試專案。</b>斷言的資料來源是測試組件自己的 <c>.deps.json</c>，
    /// 因此圖裡看得到的節點就是本專案傳遞閉包內的節點。<c>Bee.Api.AspNetCore.UnitTests</c>
    /// 是唯一同時看得到整條後端（經 <c>Bee.Api.AspNetCore</c> → <c>Bee.Hosting</c>）與
    /// <c>Bee.Api.Client</c>（經 <c>Bee.Tests.Shared</c>）的專案。後者尤其關鍵：**被禁的那個
    /// 節點必須存在於圖中**，否則「不在閉包內」會因為它根本不在圖裡而恆真。
    /// </para>
    /// <para>
    /// 手法沿用 <c>DefinitionDependencyGateTests</c>：讀 deps.json 的 <c>targets</c> 區段建圖、
    /// 從指定節點 BFS。用 deps.json 而非 <see cref="Assembly.GetReferencedAssemblies"/>，
    /// 是因為後者只反映「實際被 IL 引用」的組件，宣告了卻尚未使用的參考會漏掉 ——
    /// 而那正是本閘門要攔的東西。
    /// </para>
    /// </remarks>
    public class ArchitectureBoundaryGateTests
    {
        /// <summary>組裝層：依定義會跨越每一層，因此是唯一可以直接相依實作組件的地方。</summary>
        private const string CompositionRoot = "Bee.Hosting";

        /// <summary>「這個組件的傳遞閉包不得含有那個組件」的清單。</summary>
        public static TheoryData<string, string> ForbiddenEdges() => new()
        {
            // 1. 商業邏輯層不得相依資料存取實作 —— BO 只透過 Repository 抽象取得資料。
            { "Bee.Business", "Bee.Db" },
            { "Bee.Business", "Bee.Repository" },

            // 2. 後端不得相依 client 端函式庫。Bee.Web.Blazor.Server 相依它是正確的
            //    （那是前端 RCL），所以不在此列。
            { "Bee.Api.AspNetCore", "Bee.Api.Client" },
            { "Bee.Hosting", "Bee.Api.Client" },
            { "Bee.Api.Core", "Bee.Api.Client" },
            { "Bee.Business", "Bee.Api.Client" },
            { "Bee.Repository", "Bee.Api.Client" },
            { "Bee.Db", "Bee.Api.Client" },
        };

        [Theory]
        [MemberData(nameof(ForbiddenEdges))]
        [DisplayName("硬約束：指定組件的傳遞相依閉包不得含有被禁組件")]
        public void TransitiveClosure_DoesNotContainForbiddenAssembly(string root, string forbidden)
        {
            var graph = ReadDependencyGraph();

            // 兩個節點都必須在圖裡，否則這條斷言會因為「看不到」而恆真。
            Assert.True(graph.ContainsKey(root), $"deps.json 中找不到 {root}，閘門無從檢查。");
            Assert.True(
                graph.ContainsKey(forbidden),
                $"deps.json 中找不到 {forbidden}。被禁的節點不在圖中時，下面的斷言會恆真 —— " +
                "本測試專案必須（直接或間接）看得到它。");

            var closure = ResolveClosure(graph, root);

            Assert.False(
                closure.Contains(forbidden),
                $"{root} 的傳遞相依閉包出現 {forbidden}，違反分層硬約束。" +
                $"若這是刻意的架構變更，請同步修改 docs/development-constraints.md 與 " +
                "docs/dependency-map.md，並在此列出理由後移除該條目。");
        }

        [Fact]
        [DisplayName("硬約束：Repository 抽象不得被繞過（只有組裝層能直接相依實作）")]
        public void RepositoryImplementation_IsOnlyReferencedByTheCompositionRoot()
        {
            var graph = ReadDependencyGraph();
            const string Implementation = "Bee.Repository";
            Assert.True(graph.ContainsKey(Implementation), $"deps.json 中找不到 {Implementation}。");

            // 只看直接相依：傳遞相依是組裝層自己帶進來的，不算繞過。
            var offenders = graph
                .Where(entry => entry.Key.StartsWith("Bee.", StringComparison.Ordinal))
                .Where(entry => !IsExempt(entry.Key))
                .Where(entry => entry.Value.Contains(Implementation, StringComparer.OrdinalIgnoreCase))
                .Select(entry => entry.Key)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"下列組件直接相依 {Implementation} 而非 Bee.Repository.Abstractions：" +
                $"{string.Join(", ", offenders)}。資料存取應經抽象取得，具體實作由組裝層注入。");

            static bool IsExempt(string name)
                => string.Equals(name, CompositionRoot, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Implementation, StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".UnitTests", StringComparison.Ordinal)
                || string.Equals(name, "Bee.Tests.Shared", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [DisplayName("硬約束：Bee.Api.Contracts 只放合約，不得混入實作")]
        public void ApiContracts_ContainNoImplementation()
        {
            var assembly = typeof(Bee.Api.Contracts.System.IPingRequest).Assembly;
            var types = assembly.GetTypes().Where(type => !type.IsNested).ToArray();

            // 防空轉：型別載不到時下面的迴圈一圈都不跑。
            Assert.NotEmpty(types);

            var offenders = new List<string>();
            foreach (var type in types.Where(t => !t.IsInterface && !t.IsEnum))
            {
                // 合約軸的非介面型別只有純資料載體：屬性存取子與建構子以外的方法都算實作。
                var methods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                              | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName)
                    .Select(method => method.Name)
                    .ToArray();

                if (methods.Length > 0)
                {
                    offenders.Add($"{type.FullName}（{string.Join(", ", methods)}）");
                }
            }

            Assert.True(
                offenders.Count == 0,
                $"Bee.Api.Contracts 出現帶行為的型別：{string.Join("; ", offenders)}。" +
                "合約軸只描述形狀 —— 實作屬於 Bee.Api.Core 的訊息型別或 BO 層，混進來會讓" +
                "每個消費者都繼承到它。");
        }

        /// <summary>
        /// 回傳 <paramref name="root"/> 的傳遞相依閉包（不含自身）。
        /// </summary>
        private static HashSet<string> ResolveClosure(Dictionary<string, string[]> graph, string root)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Queue<string>(graph[root]);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                if (!visited.Add(current)) { continue; }
                if (graph.TryGetValue(current, out var next))
                {
                    foreach (var dependency in next) { pending.Enqueue(dependency); }
                }
            }
            return visited;
        }

        /// <summary>
        /// 讀取測試組件的 deps.json，回傳「library 名稱 → 直接相依名稱」的相依圖。
        /// </summary>
        private static Dictionary<string, string[]> ReadDependencyGraph()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var depsPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.deps.json");
            Assert.True(File.Exists(depsPath), $"找不到相依資訊檔：{depsPath}");

            using var document = JsonDocument.Parse(File.ReadAllText(depsPath));
            // 指定 RuntimeIdentifier 時會有兩個 target（RID-less 與 RID-specific）；取條目最多的
            // 那個，兩種建置方式下都拿得到完整圖。
            var target = document.RootElement
                .GetProperty("targets")
                .EnumerateObject()
                .OrderByDescending(entry => entry.Value.EnumerateObject().Count())
                .First()
                .Value;

            var graph = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var library in target.EnumerateObject())
            {
                // key 的格式是 "Name/Version"。
                var name = library.Name.Split('/')[0];
                graph[name] = library.Value.TryGetProperty("dependencies", out var dependencies)
                    ? dependencies.EnumerateObject().Select(entry => entry.Name).ToArray()
                    : [];
            }
            return graph;
        }
    }
}
