using System.Collections.Immutable;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// The database scope identifiers accepted by <c>FormSchema.CategoryId</c> and
    /// <c>DatabaseItem.CategoryId</c>.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: These values duplicate the constants on <c>Bee.Definition.Database.DbCategoryIds</c>.
    /// The duplication is unavoidable because analyzers target netstandard2.0 and cannot reference the
    /// net10.0 framework assembly. `DbCategoryScopesSyncTests` asserts that this list stays equal to
    /// the framework constants, so adding a scope there fails the build until it is added here too.
    /// </remarks>
    internal static class DbCategoryScopes
    {
        /// <summary>
        /// Shared system database scope.
        /// </summary>
        public const string Common = "common";

        /// <summary>
        /// Per-company business data scope.
        /// </summary>
        public const string Company = "company";

        /// <summary>
        /// Audit and operation log scope.
        /// </summary>
        public const string Log = "log";

        /// <summary>
        /// All accepted scope identifiers, in declaration order.
        /// </summary>
        public static readonly ImmutableArray<string> All = ImmutableArray.Create(Common, Company, Log);

        /// <summary>
        /// Determines whether the specified value is an accepted scope identifier.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> when the value matches a scope identifier exactly.</returns>
        public static bool IsValid(string value)
        {
            return All.Any(scope => string.Equals(value, scope, StringComparison.Ordinal));
        }

        /// <summary>
        /// Finds the accepted scope identifier that differs from the specified value only by case.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>
        /// The correctly cased scope identifier, or <c>null</c> when the value does not match any
        /// scope even case-insensitively.
        /// </returns>
        /// <remarks>
        /// Scope comparison in the framework is ordinal (see <c>DbAccessFactory</c>), so a value that
        /// differs only by case still fails to route at run time. Reporting it separately lets the
        /// diagnostic name the exact replacement instead of merely listing the accepted values.
        /// </remarks>
        public static string? FindCaseInsensitiveMatch(string value)
        {
            return All
                .Where(scope => string.Equals(value, scope, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the accepted scope identifiers formatted for use in a diagnostic message.
        /// </summary>
        /// <returns>A comma separated list, for example <c>'common', 'company', 'log'</c>.</returns>
        public static string ToDisplayList()
        {
            return "'" + string.Join("', '", All) + "'";
        }
    }
}
