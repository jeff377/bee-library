using System.Globalization;
using Bee.Api.Client.Definitions;
using Bee.UI.Core;

namespace Bee.Northwind.UI.Controls;

/// <summary>
/// Supplies the assembler that turns the raw definitions the server serves into the runtime ones a
/// view renders: a localized <c>FormSchema</c>, the tenant's layout when it has one, and the
/// company's number formats.
/// </summary>
/// <remarks>
/// The framework leaves this opt-in — a view with no loader fetches the schema exactly as stored
/// and generates a layout from it, which costs no round trips and works with no backend at all. The
/// demo opts in, because everything it wants to show lives on the other side of that switch: the
/// packaged zh-TW captions in <c>Define/Language/</c>, the tenant overrides in
/// <c>Customize/northwind-demo/</c>, and the <c>Define/FormLayout/*.xml</c> files, which a
/// generated layout ignores.
/// </remarks>
internal static class NorthwindDefinitions
{
    /// <summary>
    /// Creates a loader for one view.
    /// </summary>
    /// <remarks>
    /// The company arrives as a delegate rather than a value because the entered company changes
    /// over a session's life, and a captured value would keep baking the previous one's decimals.
    /// The default language is left empty: it only drives a fall-back hop to a second language, and
    /// the demo's English captions live on the <c>FormSchema</c> itself rather than in an en-US
    /// resource, so the hop would fetch files that are deliberately not there.
    /// </remarks>
    public static FormDefinitionLoader CreateLoader()
        => new(ClientInfo.DefineAccess) { CompanyAccessor = () => ClientInfo.Company };

    /// <summary>
    /// Resolves the language the demo renders definitions in — the UI culture, matching the
    /// framework's own default.
    /// </summary>
    public static string ResolveLang() => CultureInfo.CurrentUICulture.Name;
}
