namespace Bee.Northwind.UI.Models;

/// <summary>
/// One entry in the left navigation menu. An entry is either a non-selectable group
/// header (<see cref="IsHeader"/> is <c>true</c>) or a form link carrying the
/// <see cref="ProgId"/> that the content host opens when the entry is selected.
/// </summary>
public sealed class NavItem
{
    /// <summary>The text shown in the menu.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// The form program id opened when the entry is selected; empty for header rows.
    /// </summary>
    public string ProgId { get; init; } = string.Empty;

    /// <summary>True for a non-selectable section header.</summary>
    public bool IsHeader { get; init; }

    /// <summary>Creates a section header row.</summary>
    public static NavItem Header(string title) => new() { Title = title, IsHeader = true };

    /// <summary>Creates a selectable form link.</summary>
    public static NavItem Form(string title, string progId) => new() { Title = title, ProgId = progId };
}
