using System;
using System.Linq.Expressions;

namespace Bee.Core
{
    /// <summary>
    /// Provides a utility for resolving the full path of a property or field,
    /// converting a lambda expression to a "Class.Member" string with support for nested structures.
    /// </summary>
    public static class MemberPath
    {
        /// <summary>
        /// Gets the full path of a property or field (supports nesting).
        /// </summary>
        /// <typeparam name="T">The type of the property or field.</typeparam>
        /// <param name="expression">A lambda expression, e.g.: () => Config.Database.ServerName.</param>
        public static string Of<T>(Expression<Func<T>> expression)
        {
            if (expression.Body is MemberExpression member)
                return GetFullPath(member);

            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression innerMember)
                return GetFullPath(innerMember);

            throw new ArgumentException("Expression must be a property or field.", nameof(expression));
        }

        private static string GetFullPath(MemberExpression member)
        {
            // If there is a parent, recurse to build the full path
            if (member.Expression is MemberExpression parent)
                return $"{GetFullPath(parent)}.{member.Member.Name}";

            // Reached the top level (static class or instance class)
            return $"{member.Member.DeclaringType?.Name}.{member.Member.Name}";
        }
    }

}
