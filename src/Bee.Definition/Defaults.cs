using System.Reflection;

namespace Bee.Definition;

/// <summary>
/// Access to the framework's default define files (TableSchema / FormSchema /
/// FormLayout / Language / DbCategorySettings) embedded as manifest resources
/// in <c>Bee.Definition.dll</c>.
/// </summary>
/// <remarks>
/// <para>
/// These files describe the <c>st_*</c> system tables, the framework-shipped
/// <c>Department</c> / <c>Employee</c> forms, and the minimum
/// <c>DbCategorySettings</c> contract. Consumers use
/// <see cref="MaterializeTo(string, MaterializeOptions?)"/> at setup time (via
/// CLI or tooling) to seed their <c>DefinePath</c> from these embedded copies.
/// </para>
/// <para>
/// Runtime <see cref="Bee.Definition.Storage.IDefineStorage"/> implementations do
/// not consult this class: the framework reads only what exists on disk under
/// <see cref="PathOptions.DefinePath"/>. The contract for consumers is "have a
/// materialised copy in <c>DefinePath</c> before the app starts."
/// </para>
/// </remarks>
public static class Defaults
{
    private const string ResourcePrefix = "Bee.Definition.Defaults/";

    private static readonly Assembly s_assembly = typeof(Defaults).Assembly;

    /// <summary>
    /// Lists the relative paths of every embedded framework default file. Paths
    /// use forward slashes regardless of the host OS (e.g.
    /// <c>"TableSchema/common/st_user.TableSchema.xml"</c>).
    /// </summary>
    public static IReadOnlyList<string> ListEmbedded()
    {
        return s_assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(n => n[ResourcePrefix.Length..])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Opens the embedded stream for a single framework default file.
    /// </summary>
    /// <param name="relativePath">The relative path returned by
    /// <see cref="ListEmbedded"/>. Backslashes are normalised to forward slashes
    /// so Windows-style input also resolves.</param>
    /// <returns>A readable stream over the embedded resource. The caller owns
    /// the stream and must dispose it.</returns>
    /// <exception cref="ArgumentException"><paramref name="relativePath"/> is
    /// null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">No embedded resource matches the
    /// supplied path.</exception>
    public static Stream OpenEmbedded(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalised = relativePath.Replace('\\', '/');
        var resourceName = ResourcePrefix + normalised;
        var stream = s_assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Embedded framework default not found: '{relativePath}'. " +
                $"Use {nameof(ListEmbedded)}() to enumerate available paths.",
                relativePath);
        }
        return stream;
    }

    /// <summary>
    /// Materialises the embedded framework defaults into the given
    /// <paramref name="definePath"/> directory. The directory is created when it
    /// does not exist. By default, files already present are skipped — consumer
    /// customisations are never overwritten.
    /// </summary>
    /// <param name="definePath">The target define directory.</param>
    /// <param name="options">Materialisation options. When <c>null</c>,
    /// <see cref="MaterializeOptions.Default"/> is used.</param>
    /// <returns>A summary of which files were written and which were skipped.</returns>
    /// <exception cref="ArgumentException"><paramref name="definePath"/> is null,
    /// empty, or whitespace.</exception>
    public static MaterializeResult MaterializeTo(string definePath, MaterializeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definePath);
        options ??= MaterializeOptions.Default;

        Directory.CreateDirectory(definePath);

        var written = new List<string>();
        var skipped = new List<string>();

        foreach (var rel in ListEmbedded())
        {
            if (options.Filter != null && !options.Filter(rel))
            {
                continue;
            }

            var targetPath = Path.Combine(definePath, rel.Replace('/', Path.DirectorySeparatorChar));
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (File.Exists(targetPath) && !options.Overwrite)
            {
                skipped.Add(rel);
                continue;
            }

            using var src = OpenEmbedded(rel);
            using var dst = File.Create(targetPath);
            src.CopyTo(dst);
            written.Add(rel);
        }

        return new MaterializeResult(
            WrittenCount: written.Count,
            SkippedCount: skipped.Count,
            WrittenRelativePaths: written,
            SkippedRelativePaths: skipped);
    }
}
