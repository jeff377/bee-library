namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Fallback document for define types that do not yet have a dedicated editor,
/// and for load failures. Shows a read-only summary so the shell still gives
/// feedback when a non-supported node is selected.
/// </summary>
public sealed class UnsupportedDocumentViewModel : DocumentViewModelBase
{
    public override string Title { get; }

    public override string DocumentKey { get; }

    public string Summary { get; }

    public UnsupportedDocumentViewModel(string title, string documentKey, string summary)
    {
        Title = title;
        DocumentKey = documentKey;
        Summary = summary;
    }
}
