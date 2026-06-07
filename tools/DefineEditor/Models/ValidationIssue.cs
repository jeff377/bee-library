namespace Bee.DefineEditor.Models;

public enum ValidationSeverity
{
    Warning,
    Error,
}

/// <summary>
/// A single validation finding produced by <see cref="Services.FormSchemaValidator"/>.
/// </summary>
public sealed record ValidationIssue(ValidationSeverity Severity, string Path, string Message)
{
    public string Icon => Severity switch
    {
        ValidationSeverity.Error => "❌",
        _ => "⚠️",
    };
}
