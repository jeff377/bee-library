namespace Bee.Definition.Logging
{
    /// <summary>
    /// A single column name/value pair contributed by an <see cref="AuditEntry"/> when it is
    /// materialised into an INSERT. A null <see cref="Value"/> is written as SQL NULL.
    /// </summary>
    /// <param name="Name">The log-table column name (e.g. <c>log_time</c>).</param>
    /// <param name="Value">The column value; null maps to SQL NULL.</param>
    public readonly record struct AuditColumn(string Name, object? Value);
}
