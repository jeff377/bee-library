using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// 相依閘門：斷言 <c>Bee.Definition</c> 的**傳遞相依閉包**落在白名單內。
    /// </summary>
    /// <remarks>
    /// <para>
    /// adr-036 立下的判準是「會不會讓定義層長出外部套件相依」，但當時是以人眼 grep 關鍵字落實，
    /// 因此漏掉了經 <c>Bee.Expressions</c> 傳遞進來的 <c>DynamicExpresso.Core</c>。本測試把該判準
    /// 變成可執行檢查：新增任何外部相依都必須顯式加進白名單，逼出一次決策而非默默通過。
    /// </para>
    /// <para>
    /// 資料來源是**本測試組件自己的 <c>.deps.json</c>**，而非 <c>Bee.Definition.deps.json</c> ——
    /// 後者不會被複製到測試輸出目錄。deps.json 的 <c>targets</c> 區段逐一記錄每個 library 的直接
    /// 相依邊，故從 <c>Bee.Definition</c> 這個節點做 BFS 得到的正是它自己的閉包，不受本測試專案
    /// 另外引用了哪些專案影響。用 deps.json 而非
    /// <see cref="Assembly.GetReferencedAssemblies"/>，是因為後者只反映「實際被 IL 引用」的組件，
    /// 宣告了卻尚未使用的套件相依會漏掉 —— 而那正是本閘門要攔的東西。
    /// </para>
    /// <para>
    /// 閉包中不會出現 BCL：框架組件由共用框架（Microsoft.NETCore.App）解析，不列入 deps.json 的
    /// library 清單。標了 <c>PrivateAssets="all"</c> 的建置期套件（SourceLink、analyzer）同理。
    /// </para>
    /// </remarks>
    public class DefinitionDependencyGateTests
    {
        /// <summary>
        /// 待檢查的定義層組件名稱。
        /// </summary>
        private const string RootLibrary = "Bee.Definition";

        /// <summary>
        /// 允許出現在 <see cref="RootLibrary"/> 傳遞相依閉包中的組件／套件。
        /// </summary>
        /// <remarks>
        /// <c>Microsoft.Extensions.Localization.Abstractions</c>（由
        /// <c>Language/BeeStringLocalizer.cs</c> 使用）是 Microsoft 第一方的純抽象套件、隨 .NET
        /// 版本走，不帶實作也不鎖定任何引擎，故列入白名單。第三方實作套件則不得出現在此。
        /// </remarks>
        private static readonly HashSet<string> s_allowedDependencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "Bee.Base",
            "Microsoft.Extensions.Localization.Abstractions",
        };

        [Fact]
        [DisplayName("Bee.Definition 的傳遞相依不得超出白名單")]
        public void TransitiveDependencies_StayWithinWhitelist()
        {
            var closure = ResolveDependencyClosure(RootLibrary);

            var unexpected = closure
                .Where(name => !s_allowedDependencies.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                unexpected.Length == 0,
                $"{RootLibrary} 出現白名單外的傳遞相依：{string.Join(", ", unexpected)}。" +
                "外部套件相依不應洩漏到定義層（adr-036 判準）；若這是刻意決策，請在本測試的白名單中" +
                "顯式加入並於 ADR 說明理由。");
        }

        [Fact]
        [DisplayName("相依閘門確實走到了 Bee.Definition 的相依邊")]
        public void DependencyClosure_IsNotVacuous()
        {
            // 若 deps.json 的節點名稱有變、BFS 起點解析失敗而回傳空集合，上面那條會「無條件通過」。
            // 這條把「閘門有在看東西」本身也變成斷言。
            var closure = ResolveDependencyClosure(RootLibrary);

            Assert.Contains("Bee.Base", closure, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 從測試組件的 deps.json 讀出相依圖，並回傳 <paramref name="rootLibrary"/> 的傳遞相依閉包
        /// （不含自身）。
        /// </summary>
        /// <param name="rootLibrary">起點 library 名稱（不含版本）。</param>
        private static IReadOnlyCollection<string> ResolveDependencyClosure(string rootLibrary)
        {
            var graph = ReadDependencyGraph();

            Assert.True(
                graph.ContainsKey(rootLibrary),
                $"deps.json 中找不到 {rootLibrary} 這個 library，相依閘門無從檢查。");

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Queue<string>(graph[rootLibrary]);
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
