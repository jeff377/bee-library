using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Base type for the right-pane document editors. Every concrete editor (FormSchema,
/// singleton settings, etc.) inherits this so the shell can host them through the
/// <see cref="Avalonia.Controls.TabControl"/> in MainWindow with views resolved via
/// <see cref="ViewLocator"/>.
/// </summary>
public abstract partial class DocumentViewModelBase : ViewModelBase
{
    /// <summary>Header shown in the tab and inside the document.</summary>
    public abstract string Title { get; }

    /// <summary>
    /// Stable identity used by the shell to detect "already open" — typically the
    /// canonical file path. Two documents with the same key are treated as the
    /// same tab; selecting the source node activates it rather than re-loading.
    /// </summary>
    public abstract string DocumentKey { get; }

    /// <summary>
    /// Tracks unsaved edits so the tab can show a modified indicator. Concrete
    /// editors set this when their underlying schema is mutated and clear it on
    /// successful save.
    /// </summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>Optional one-character emoji shown left of the title in the tab.</summary>
    public virtual string TabIcon => "📄";
}
