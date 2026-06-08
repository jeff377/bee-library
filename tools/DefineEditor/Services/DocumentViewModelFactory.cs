using System;
using System.IO;
using Bee.Definition;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.ViewModels;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Builds the appropriate <see cref="DocumentViewModelBase"/> for a selected
/// <see cref="DefineNode"/>. Returns <c>null</c> when nothing actionable is
/// selected (no node, or a grouping node).
/// </summary>
public static class DocumentViewModelFactory
{
    public static DocumentViewModelBase? Create(DefineNode? node, SolutionContext solution)
    {
        if (node is null || node.Kind != DefineNodeKind.DefineFile || node.FilePath is null)
            return null;

        return node.DefineType switch
        {
            DefineType.FormSchema => LoadOrFallback(node, () => FormSchemaDocumentViewModel.Load(node.FilePath!, solution)),
            DefineType.PermissionModels => LoadOrFallback(node, () => PermissionModelsDocumentViewModel.Load(node.FilePath!)),
            DefineType.DbCategorySettings => LoadOrFallback(node, () => DbCategorySettingsDocumentViewModel.Load(node.FilePath!)),
            DefineType.ProgramSettings => LoadOrFallback(node, () => ProgramSettingsDocumentViewModel.Load(node.FilePath!)),
            DefineType.SystemSettings => LoadOrFallback(node, () => SystemSettingsDocumentViewModel.Load(node.FilePath!)),
            DefineType.DatabaseSettings => LoadOrFallback(node, () => DatabaseSettingsDocumentViewModel.Load(node.FilePath!)),
            DefineType.TableSchema => LoadOrFallback(node, () => TableSchemaDocumentViewModel.Load(node.FilePath!)),
            DefineType.FormLayout => LoadOrFallback(node, () => FormLayoutDocumentViewModel.Load(node.FilePath!)),
            DefineType.Language => LoadOrFallback(node, () => LanguageDocumentViewModel.Load(node.FilePath!)),
            _ => Unsupported(node, "An editor for this define type is not implemented yet."),
        };
    }

    private static DocumentViewModelBase LoadOrFallback(DefineNode node, Func<DocumentViewModelBase> loader)
    {
        try { return loader(); }
        catch (Exception ex) when (ex is IOException
                                 or InvalidOperationException
                                 or UnauthorizedAccessException
                                 or FileNotFoundException)
        {
            return Unsupported(node, $"Load failed: {ex.Message}");
        }
    }

    private static UnsupportedDocumentViewModel Unsupported(DefineNode node, string note)
    {
        var summary = string.Join(Environment.NewLine,
            $"Type: {node.DefineType}",
            $"Key: {node.KeyText ?? "(singleton)"}",
            $"Path: {node.FilePath}",
            string.Empty,
            note);
        return new UnsupportedDocumentViewModel(node.Name, node.FilePath ?? node.Name, summary);
    }
}
