using System.Reflection;
using Bee.Definition.Attributes;
using Bee.Definition.Security;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.Validator
{
    /// <summary>
    /// Provides access validation logic for API methods, supporting access conditions inherited from base method definitions.
    /// </summary>
    public static class ApiAccessValidator
    {
        /// <summary>
        /// Validates whether the specified method satisfies the access conditions (local, encoded, encrypted),
        /// and throws an exception if the conditions are not met.
        /// </summary>
        /// <param name="method">The API method to validate.</param>
        /// <param name="context">The current API call context.</param>
        /// <param name="tokenValidator">The access-token validator used when the method requires authentication.</param>
        /// <remarks>
        /// IMPORTANT: A method that no <see cref="ApiAccessControlAttribute"/> covers is <b>denied</b>, not
        /// treated as unrestricted — the absence of a declaration rejects the call rather than permitting
        /// it. The attribute is resolved from the method, then the method it overrides, then the declaring
        /// type, so a type-level attribute covers all of its methods. Analyzer rule BEE3001 reports an
        /// uncovered public business object method at build time, before a client discovers it.
        /// </remarks>
        public static void ValidateAccess(MethodInfo method, ApiCallContext context, IAccessTokenValidator tokenValidator)
        {
            ArgumentNullException.ThrowIfNull(tokenValidator);

            var attr = FindAccessAttribute(method);
            if (attr == null)
            {
                throw new UnauthorizedAccessException(
                    $"API method '{method.DeclaringType?.FullName}.{method.Name}' is not accessible without {nameof(ApiAccessControlAttribute)}.");
            }

            // Local calls are allowed regardless of protection level
            if (context.IsLocalCall)
                return;

            // Check whether an AccessToken is required
            if (attr.AccessRequirement == ApiAccessRequirement.Authenticated && !IsTokenValid(context.AccessToken, tokenValidator))
                throw new UnauthorizedAccessException("AccessToken is required or invalid.");

            if (attr.ProtectionLevel == ApiProtectionLevel.LocalOnly && !context.IsLocalCall)
                throw new UnauthorizedAccessException("This API is restricted to local calls only.");

            // Validate the access level based on the caller's payload format
            switch (context.Format)
            {
                case PayloadFormat.Encrypted:
                    // Encrypted calls may invoke any non-LocalOnly API
                    return;

                case PayloadFormat.Encoded:
                    if (attr.ProtectionLevel > ApiProtectionLevel.Encoded)
                        throw new UnauthorizedAccessException("This API requires encrypted transmission.");
                    return;

                default:
                    // Plain (and any other value) requires ProtectionLevel.Public
                    if (attr.ProtectionLevel > ApiProtectionLevel.Public)
                        throw new UnauthorizedAccessException("This API requires encoded or encrypted transmission.");
                    return;
            }
        }

        /// <summary>
        /// Resolves the access-control declaration that governs a method, following the same
        /// method → base method → declaring type priority that <see cref="ValidateAccess"/> uses.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <returns>The governing attribute, or null when the method is covered by none.</returns>
        public static ApiAccessControlAttribute? FindAccessControl(MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return FindAccessAttribute(method);
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="ApiAccessControlAttribute"/> using the following priority:
        /// 1. Directly on the method.
        /// 2. On the base method definition (override inheritance).
        /// 3. On the declaring class (class-level default).
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <returns>The attribute if found; otherwise, null.</returns>
        private static ApiAccessControlAttribute? FindAccessAttribute(MethodInfo method)
        {
            var attr = method.GetCustomAttribute<ApiAccessControlAttribute>();
            if (attr != null)
                return attr;

            var baseMethod = method.GetBaseDefinition();
            if (baseMethod != method)
            {
                attr = baseMethod.GetCustomAttribute<ApiAccessControlAttribute>();
                if (attr != null)
                    return attr;
            }

            return method.DeclaringType?.GetCustomAttribute<ApiAccessControlAttribute>();
        }

        /// <summary>
        /// Validates the AccessToken. Returns false if the token is empty or invalid.
        /// </summary>
        private static bool IsTokenValid(Guid accessToken, IAccessTokenValidator tokenValidator)
        {
            if (accessToken == Guid.Empty)
                return false;

            return tokenValidator.Validate(accessToken);
        }
    }

}
