using System;
using Newtonsoft.Json.Serialization;

namespace Bee.Core.Serialization
{
    /// <summary>
    /// Validates type legality during JSON serialization.
    /// </summary>
    internal class JsonSerializationBinder : ISerializationBinder
    {
        private static readonly DefaultSerializationBinder Binder = new DefaultSerializationBinder();
        private static readonly bool IsNetCore = !System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET Framework");

        /// <summary>
        /// Controls the binding between serialized objects and types.
        /// </summary>
        /// <param name="serializedType">The serialized object type.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The type name.</param>
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            Binder.BindToName(serializedType, out assemblyName, out typeName);
        }

        /// <summary>
        /// Controls the binding between serialized objects and types.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The type name.</param>
        public Type BindToType(string assemblyName, string typeName)
        {
            // .NET Framework 4.8 → .NET 8
            if (IsNetCore && assemblyName == "mscorlib")
            {
                assemblyName = "System.Private.CoreLib";
            }
            // .NET 8 → .NET Framework 4.8
            else if (!IsNetCore && assemblyName == "System.Private.CoreLib")
            {
                assemblyName = "mscorlib";
            }

            if (!ValidateType(typeName))
                throw new InvalidOperationException($"Type name '{typeName}' is not allowed.");

            return Binder.BindToType(assemblyName, typeName);
        }

        /// <summary>
        /// Validates the legality of a serialized type.
        /// </summary>
        /// <param name="typeName">The type name to validate.</param>
        private bool ValidateType(string typeName)
        {
            return SysInfo.IsTypeNameAllowed(typeName);
        }
    }
}
