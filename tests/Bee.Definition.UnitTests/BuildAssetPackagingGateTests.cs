using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// 打包閘門：斷言 <c>Bee.Definition.targets</c> 以 <c>buildTransitive/</c> 發布，
    /// 而非只對直接消費者生效的 <c>build/</c>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// NuGet 只對**直接** <c>PackageReference</c> 匯入套件的 <c>build/</c> 資料夾。這支 targets 的
    /// 職責是把消費端的定義檔注入 <c>AdditionalFiles</c>，供 BEE1xxx / BEE2xxx 讀取；放在
    /// <c>build/</c> 時，凡是只引用 <c>Bee.Business</c> / <c>Bee.Db</c> / <c>Bee.Api.AspNetCore</c> /
    /// <c>Bee.Hosting</c> 的專案（<c>Bee.Definition</c> 為遞移相依）都拿不到它。
    /// </para>
    /// <para>
    /// 這個失效**完全沒有症狀**：analyzer 組件本身會遞移流入並正常載入，只是讀不到任何檔案，
    /// 於是規則靜默通過，不會有任何診斷指出這件事。實際發生過一次，且是由外部 repo
    /// （bee-northwind-avalonia，4.21.0）實測 <c>AdditionalFiles</c> 為 0 筆才發現的。
    /// </para>
    /// <para>
    /// 本測試檢查的是 repository 的原始檔佈局與 <c>Pack</c> 宣告，而不是實際打出來的 nupkg ——
    /// 在單元測試裡跑一次 <c>dotnet pack</c> 太慢。要攔的回歸（把資料夾或 <c>PackagePath</c> 改回
    /// <c>build</c>）在這一層就看得見。
    /// </para>
    /// </remarks>
    public class BuildAssetPackagingGateTests
    {
        /// <summary>
        /// MSBuild targets 檔必須位於此資料夾，才會對遞移消費者生效。
        /// </summary>
        private const string RequiredFolder = "buildTransitive";

        [Fact]
        [DisplayName("Bee.Definition.targets 位於 buildTransitive 而非 build 資料夾")]
        public void DefinitionTargets_LiveUnderBuildTransitive()
        {
            var projectDir = GetDefinitionProjectDirectory();

            Assert.True(
                File.Exists(Path.Combine(projectDir, RequiredFolder, "Bee.Definition.targets")),
                $"找不到 {RequiredFolder}/Bee.Definition.targets。這支 targets 必須放在 " +
                $"{RequiredFolder}/，否則只有直接引用 Bee.Definition 的專案會匯入它。");

            Assert.False(
                Directory.Exists(Path.Combine(projectDir, "build")),
                "src/Bee.Definition/build/ 不應存在。NuGet 只對直接 PackageReference 匯入 build/，" +
                $"而 {RequiredFolder}/ 對直接與遞移消費者都生效，故不需要第二份。");
        }

        [Fact]
        [DisplayName("csproj 把 targets 打包到 buildTransitive 路徑")]
        public void DefinitionCsproj_PacksTargetsToBuildTransitive()
        {
            var csproj = File.ReadAllText(
                Path.Combine(GetDefinitionProjectDirectory(), "Bee.Definition.csproj"));

            Assert.Contains($"PackagePath=\"{RequiredFolder}\\\"", csproj, StringComparison.Ordinal);

            Assert.DoesNotContain("PackagePath=\"build\\\"", csproj, StringComparison.Ordinal);
        }

        /// <summary>
        /// 取得 <c>src/Bee.Definition</c> 的絕對路徑。
        /// </summary>
        private static string GetDefinitionProjectDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && dir.GetDirectories(".git").Length == 0)
            {
                dir = dir.Parent;
            }

            Assert.True(dir != null, "自測試輸出目錄往上找不到 repository 根目錄（.git）。");

            var projectDir = Path.Combine(dir!.FullName, "src", "Bee.Definition");
            Assert.True(Directory.Exists(projectDir), $"找不到 {projectDir}。");

            return projectDir;
        }
    }
}
