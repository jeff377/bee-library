using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.UnitTests
{
    /// <summary>
    /// 以最小 compilation 驅動 analyzer 並取回診斷結果的測試 harness。
    /// </summary>
    /// <remarks>
    /// 定義檔規則（BEE1xxx / BEE2xxx）不依賴任何 C# 型別，故 compilation 僅需一個 placeholder
    /// 語法樹與 runtime 的 <c>System.Private.CoreLib</c>；不從 NuGet 解析參考組件，測試因此可離線執行。
    /// </remarks>
    internal static class AnalyzerRunner
    {
        private const string PlaceholderSource = "internal sealed class Placeholder { }";

        /// <summary>
        /// 以指定的定義檔執行 analyzer，回傳其產生的診斷。
        /// </summary>
        /// <param name="analyzer">要執行的 analyzer。</param>
        /// <param name="additionalFiles">模擬的定義檔（路徑與內容）。</param>
        /// <returns>analyzer 產生的診斷，依 ID 與位置排序。</returns>
        public static ImmutableArray<Diagnostic> Run(
            DiagnosticAnalyzer analyzer,
            params (string Path, string Content)[] additionalFiles)
            => Run(analyzer, PlaceholderSource, additionalFiles);

        /// <summary>
        /// 以指定的原始碼執行 analyzer，回傳其產生的診斷。
        /// </summary>
        /// <param name="analyzer">要執行的 analyzer。</param>
        /// <param name="source">要分析的 C# 原始碼。</param>
        /// <param name="anchorTypes">
        /// 原始碼所引用之外部型別的代表（如 <c>MessagePack.KeyAttribute</c>），用於確保其組件進入參考集。
        /// </param>
        /// <returns>analyzer 產生的診斷。</returns>
        /// <remarks>
        /// IMPORTANT: 需要外部 attribute 的規則必須傳入對應 anchor。僅靠 <c>AppDomain</c> 已載入組件
        /// 並不可靠——組件載入是惰性的，單獨執行某個測試時該組件可能尚未載入，導致原始碼編譯出
        /// error type、規則靜默不觸發，徵狀與「規則有 bug」完全相同。
        /// </remarks>
        public static ImmutableArray<Diagnostic> RunOnSource(
            DiagnosticAnalyzer analyzer,
            string source,
            params Type[] anchorTypes)
            => Run(analyzer, source, BuildReferences(anchorTypes), []);

        /// <summary>
        /// 以指定的原始碼與定義檔執行 analyzer，回傳其產生的診斷。
        /// </summary>
        /// <param name="analyzer">要執行的 analyzer。</param>
        /// <param name="source">要分析的 C# 原始碼。</param>
        /// <param name="additionalFiles">模擬的定義檔（路徑與內容）。</param>
        /// <returns>analyzer 產生的診斷。</returns>
        public static ImmutableArray<Diagnostic> Run(
            DiagnosticAnalyzer analyzer,
            string source,
            params (string Path, string Content)[] additionalFiles)
            => Run(analyzer, source, [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)], additionalFiles);

        /// <summary>
        /// 取得指定原始碼的編譯期診斷，用於排查測試素材本身是否有編譯錯誤。
        /// </summary>
        /// <param name="source">要編譯的 C# 原始碼。</param>
        /// <param name="anchorTypes">原始碼所引用之外部型別的代表。</param>
        /// <returns>編譯期的錯誤與警告。</returns>
        /// <remarks>
        /// 語意分析規則遇到 error type 會靜默失效（找不到 attribute 符號），徵狀與「規則未觸發」
        /// 完全相同。測試素材有編譯錯誤時，此方法可直接區分兩者。
        /// </remarks>
        public static ImmutableArray<Diagnostic> GetCompilationDiagnostics(string source, params Type[] anchorTypes)
        {
            return CSharpCompilation.Create(
                assemblyName: "Bee.Analyzers.TestAssembly",
                syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
                references: BuildReferences(anchorTypes),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .GetDiagnostics();
        }

        /// <summary>
        /// 組出參考集：anchor 型別所屬組件，加上目前已載入的非動態組件。
        /// </summary>
        /// <param name="anchorTypes">要確保納入的型別。</param>
        /// <returns>去重後的組件參考。</returns>
        private static IEnumerable<MetadataReference> BuildReferences(Type[] anchorTypes)
        {
            var locations = new HashSet<string>(StringComparer.Ordinal);

            // 存取 Assembly 會強制載入該組件，故 anchor 必須先加入。
            foreach (var type in anchorTypes)
            {
                if (!string.IsNullOrEmpty(type.Assembly.Location))
                    locations.Add(type.Assembly.Location);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                    locations.Add(assembly.Location);
            }

            return locations.Select(location => (MetadataReference)MetadataReference.CreateFromFile(location)).ToArray();
        }

        private static ImmutableArray<Diagnostic> Run(
            DiagnosticAnalyzer analyzer,
            string source,
            IEnumerable<MetadataReference> references,
            (string Path, string Content)[] additionalFiles)
        {
            var compilation = CSharpCompilation.Create(
                assemblyName: "Bee.Analyzers.TestAssembly",
                syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var texts = additionalFiles
                .Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Content))
                .ToImmutableArray();

            var withAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.Create(analyzer),
                new AnalyzerOptions(texts));

            return withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
    }
}
