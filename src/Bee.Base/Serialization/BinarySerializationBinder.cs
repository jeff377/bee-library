using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Validates type legitimacy during binary deserialization to prevent unsafe type loading.
    /// </summary>
    internal class BinarySerializationBinder : SerializationBinder
    {
        /// <summary>
        /// Controls the binding between serialized objects and their types.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The type name.</param>
        public override Type BindToType(string assemblyName, string typeName)
        {
            // Validate that the type is allowed
            if (!ValidateType(typeName))
                throw new InvalidOperationException($"Type name '{typeName}' is not allowed.");
            // Return the resolved serialization type
            var type = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
            return type;
        }

        /// <summary>
        /// List of allowed type namespaces for data passed over JSON-RPC.
        /// Only types within these namespaces are permitted for deserialization to ensure security.
        /// Note: Bee.Base and Bee.Define are built-in default namespaces and do not need to be specified here.
        /// </summary>
        private static List<string> AllowedTypeNamespaces { get; set; } = new List<string> {
            "System.Collections", "System.Globalization", "System.Data"
        };

        /// <summary>
        /// Set of explicitly allowed type names.
        /// </summary>
        private static readonly HashSet<string> AllowedTypes = new HashSet<string>
        {
                "System.Byte[]", "System.Guid", "System.CultureAwareComparer",
                "System.Version", "System.UnitySerializationHolder"
        };

        /// <summary>
        /// Validates whether the specified type name is permitted for deserialization.
        /// </summary>
        /// <param name="typeName">The type name to validate.</param>
        private bool ValidateType(string typeName)
        {
            // Check against the globally allowed type list
            if (SysInfo.IsTypeNameAllowed(typeName)) { return true; }

            // Binary-serialization-specific: check allowed namespaces
            foreach (var ns in AllowedTypeNamespaces)
            {
                if (typeName.StartsWith(ns + "."))
                    return true;
            }
            // Binary-serialization-specific: check allowed types set
            return AllowedTypes.Contains(typeName);
        }
    }
}
