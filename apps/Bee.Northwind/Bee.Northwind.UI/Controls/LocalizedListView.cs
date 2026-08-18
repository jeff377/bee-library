using Bee.Api.Client.Definitions;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Views;

namespace Bee.Northwind.UI.Controls;

/// <summary>
/// A <see cref="ListView"/> whose column headers come from the localized, tenant-customized schema
/// rather than the schema as stored.
/// </summary>
/// <remarks>
/// <see cref="FormView"/> exposes the assembler as a settable <c>DefinitionLoader</c> property;
/// <see cref="ListView"/> does not, and offers <c>ResolveSchemaAsync</c> as its substitution point
/// instead, so a host that wants localized headers subclasses. Without this the two surfaces
/// disagree — the record form would read 經銷商 while the list it opened from still read Customer
/// Name — which reads as a bug rather than as a demonstration.
/// </remarks>
public sealed class LocalizedListView : ListView
{
    private readonly FormDefinitionLoader _loader = NorthwindDefinitions.CreateLoader();

    /// <inheritdoc/>
    protected override async Task<FormSchema?> ResolveSchemaAsync(string progId)
        => await _loader
            .GetLocalizedSchemaAsync(progId, NorthwindDefinitions.ResolveLang())
            .ConfigureAwait(false);
}
