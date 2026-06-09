namespace Bee.Definition;

/// <summary>
/// Options for <see cref="Defaults.MaterializeTo(string, MaterializeOptions?)"/>.
/// </summary>
public sealed class MaterializeOptions
{
    /// <summary>
    /// Gets the default options instance: skip existing files; no filter.
    /// </summary>
    public static MaterializeOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether to overwrite files that already exist in
    /// the target define path. The default is <c>false</c> so consumer
    /// customisations are never lost.
    /// </summary>
    public bool Overwrite { get; init; }

    /// <summary>
    /// Gets an optional filter applied to each candidate relative path (forward-slash
    /// separator). Return <c>true</c> to include the file, <c>false</c> to skip it.
    /// When <c>null</c>, all embedded files are considered.
    /// </summary>
    public Predicate<string>? Filter { get; init; }
}
