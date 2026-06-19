using Avalonia.Controls;

namespace Avalonia.DemoCenter.Modules
{
    /// <summary>
    /// A self-contained demo scenario: where it sits in the navigation tree
    /// (<see cref="Category"/> → <see cref="ControlName"/> → <see cref="ScenarioTitle"/>),
    /// a short description, a factory that builds the live interactive view, and the
    /// real source text shown in the View Source panel.
    /// </summary>
    public interface IDemoModule
    {
        /// <summary>Top-level navigation group, e.g. "Data Editors" / "Grid" / "Views".</summary>
        string Category { get; }

        /// <summary>The control (or scenario family) this module demonstrates, e.g. "TextEdit".</summary>
        string ControlName { get; }

        /// <summary>The scenario title shown as the navigation leaf, e.g. "唯讀 vs 編輯".</summary>
        string ScenarioTitle { get; }

        /// <summary>One-line description shown in the scenario header.</summary>
        string Description { get; }

        /// <summary>Builds the live, interactive view for this scenario.</summary>
        Control BuildView();

        /// <summary>
        /// Returns the module's own source text for the View Source panel. Implementations
        /// read their real <c>.cs</c> from an embedded resource so the displayed source can
        /// never drift from what runs.
        /// </summary>
        string GetSourceText();
    }
}
