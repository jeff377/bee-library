namespace Bee.Definition;

/// <summary>
/// Outcome of a <see cref="Defaults.MaterializeTo(string, MaterializeOptions?)"/> call.
/// </summary>
/// <param name="WrittenCount">Number of files actually written to disk.</param>
/// <param name="SkippedCount">Number of files skipped because they already existed
/// (and <see cref="MaterializeOptions.Overwrite"/> was <c>false</c>) or were excluded
/// by <see cref="MaterializeOptions.Filter"/>.</param>
/// <param name="WrittenRelativePaths">Relative paths (forward-slash separator) of
/// the written files, in lexicographic order.</param>
/// <param name="SkippedRelativePaths">Relative paths (forward-slash separator) of
/// the skipped files, in lexicographic order.</param>
public sealed record MaterializeResult(
    int WrittenCount,
    int SkippedCount,
    IReadOnlyList<string> WrittenRelativePaths,
    IReadOnlyList<string> SkippedRelativePaths);
