namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Base type for the right-pane document editors. Every concrete editor (FormSchema,
/// singleton settings, etc.) inherits this so the shell can host them through a
/// single <see cref="Avalonia.Controls.ContentControl"/> resolved via <see cref="ViewLocator"/>.
/// </summary>
public abstract class DocumentViewModelBase : ViewModelBase
{
    /// <summary>Header shown above the document content.</summary>
    public abstract string Title { get; }
}
