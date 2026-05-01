using System.Reflection;
using System.Text.Json;

namespace Bee.Api.Core.Conversion
{
    /// <summary>
    /// Converts API request objects to BO parameter types by copying matching properties.
    /// Used when the Executor receives an API type (e.g., LoginRequest) but the BO method
    /// expects a different type (e.g., ILoginRequest or LoginArgs).
    /// </summary>
    public static class ApiInputConverter
    {
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Converts the source object to the specified target type by copying public properties with matching names.
        /// If the source is a <see cref="JsonElement"/> (from JSON deserialization), it is deserialized directly.
        /// </summary>
        /// <param name="source">The source object (typically an API request type).</param>
        /// <param name="targetType">The target type to convert to (typically a BO args type or interface).</param>
        /// <returns>A new instance of the target type with matching properties copied from the source.</returns>
        public static object? Convert(object source, Type targetType)
        {
            if (source == null) return null;

            // If the source is already assignable to the target type, return as-is
            if (targetType.IsInstanceOfType(source))
                return source;

            // Handle JsonElement (from JSON deserialization over HTTP).
            // PropertyNameCaseInsensitive is required because the framework serializes
            // with camelCase naming policy (see JsonCodec internal options).
            if (source is JsonElement element)
            {
                return JsonSerializer.Deserialize(element.GetRawText(), targetType, CaseInsensitiveOptions);
            }

            // If the target is an interface, we cannot create an instance directly
            if (targetType.IsInterface || targetType.IsAbstract)
                return source;

            var target = Activator.CreateInstance(targetType)!;
            var sourceType = source.GetType();

            // Copy all public instance properties from source to target
            foreach (var targetProp in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!targetProp.CanWrite) continue;

                var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (sourceProp != null && sourceProp.CanRead && targetProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    targetProp.SetValue(target, sourceProp.GetValue(source));
                }
            }

            return target;
        }
    }
}
