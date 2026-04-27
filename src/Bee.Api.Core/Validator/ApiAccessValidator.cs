using System.Reflection;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

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
        /// If the method is not marked with <see cref="ApiAccessControlAttribute"/>, access is treated as unrestricted.
        /// </summary>
        /// <param name="method">The API method to validate.</param>
        /// <param name="context">The current API call context.</param>
        public static void ValidateAccess(MethodInfo method, ApiCallContext context)
        {
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
            if (attr.AccessRequirement == ApiAccessRequirement.Authenticated && !IsTokenValid(context.AccessToken))
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
        /// Attempts to retrieve the <see cref="ApiAccessControlAttribute"/> from the method or its base definition.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <returns>The attribute if found; otherwise, null.</returns>
        private static ApiAccessControlAttribute? FindAccessAttribute(MethodInfo method)
        {
            var attr = method.GetCustomAttribute<ApiAccessControlAttribute>();
            if (attr != null)
                return attr;

            var baseMethod = method.GetBaseDefinition();
            return baseMethod != method
                ? baseMethod.GetCustomAttribute<ApiAccessControlAttribute>()
                : null;
        }

        /// <summary>
        /// Validates the AccessToken. Returns false if the token is empty or invalid.
        /// </summary>
        private static bool IsTokenValid(Guid accessToken)
        {
            if (accessToken == Guid.Empty)
                return false;

            var provider = BackendInfo.AccessTokenValidationProvider;
            if (provider == null)
                throw new InvalidOperationException("AccessTokenValidationProvider is not configured.");

            return provider.ValidateAccessToken(accessToken);
        }
    }

}
