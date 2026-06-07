namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Fallback document for define types that do not yet have a dedicated editor.
/// Shows a read-only summary so the shell still gives feedback when a non-FormSchema
/// node is selected.
/// </summary>
public sealed class UnsupportedDocumentViewModel : DocumentViewModelBase
{
    public override string Title { get; }

    public string Summary { get; }

    public UnsupportedDocumentViewModel(string title, string summary)
    {
        Title = title;
        Summary = summary;
    }
}
