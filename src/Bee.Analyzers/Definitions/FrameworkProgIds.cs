using System.Collections.Immutable;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// The program identifiers whose form schemas ship with the framework itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These schemas are embedded resources in <c>Bee.Definition</c>, not files in the consumer's
    /// definition folder. A consumer can therefore reference them from a relation field without having
    /// a corresponding file for the analyzers to see, so rules resolving program identifiers must treat
    /// them as known rather than missing.
    /// </para>
    /// <para>
    /// IMPORTANT: This list duplicates what the framework embeds. <c>FrameworkProgIdsSyncTests</c>
    /// asserts it against <c>Defaults.ListEmbedded()</c>, so shipping another built-in schema fails the
    /// build until it is added here too.
    /// </para>
    /// </remarks>
    internal static class FrameworkProgIds
    {
        /// <summary>
        /// All program identifiers with a framework-supplied form schema.
        /// </summary>
        public static readonly ImmutableArray<string> All = ImmutableArray.Create("AuditRule", "Department", "Employee");

        /// <summary>
        /// Determines whether the framework supplies a form schema for the specified program identifier.
        /// </summary>
        /// <param name="progId">The program identifier to test.</param>
        /// <returns><c>true</c> when the schema ships with the framework.</returns>
        public static bool IsFrameworkSupplied(string progId)
        {
            return All.Contains(progId, StringComparer.OrdinalIgnoreCase);
        }
    }
}
