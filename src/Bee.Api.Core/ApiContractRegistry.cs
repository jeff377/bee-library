using System.Collections.Concurrent;
using System.Reflection;
using MessagePack;

namespace Bee.Api.Core
{
    /// <summary>
    /// Registry for mapping contract interfaces to API response types.
    /// When a BO method returns a pure POCO that implements a contract interface,
    /// this registry maps it to the corresponding API type (with MessagePack attributes) for serialization.
    /// </summary>
    public static class ApiContractRegistry
    {
        private static readonly ConcurrentDictionary<Type, ContractMapping> _mappings = new ConcurrentDictionary<Type, ContractMapping>();

        /// <summary>
        /// Registers a mapping from a contract interface to its API response type.
        /// </summary>
        /// <typeparam name="TContract">The contract interface type (e.g., ILoginResponse).</typeparam>
        /// <typeparam name="TApi">The API type with MessagePack attributes (e.g., LoginResponse).</typeparam>
        public static void Register<TContract, TApi>()
            where TApi : TContract, new()
        {
            var contractType = typeof(TContract);
            var apiType = typeof(TApi);
            var properties = contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _mappings[contractType] = new ContractMapping(apiType, properties);
        }

        /// <summary>
        /// Attempts to convert a value to its registered API type for serialization.
        /// Returns the original value if no mapping is needed (type already has MessagePackObject attribute).
        /// </summary>
        /// <param name="value">The value to potentially convert.</param>
        /// <returns>The converted API object, or the original value if no conversion is needed.</returns>
        public static object? ConvertForSerialization(object value)
        {
            if (value == null) return null;

            var valueType = value.GetType();

            // If the type already has MessagePackObject attribute, no conversion needed
            if (valueType.GetCustomAttribute<MessagePackObjectAttribute>() != null)
                return value;

            // Search for a registered contract interface on the value's type
            foreach (var iface in valueType.GetInterfaces())
            {
                if (_mappings.TryGetValue(iface, out var mapping))
                {
                    return mapping.Convert(value);
                }
            }

            // No mapping found, return as-is (will use existing serialization path)
            return value;
        }

        private sealed class ContractMapping
        {
            private readonly Type _apiType;
            private readonly PropertyInfo[] _contractProperties;

            public ContractMapping(Type apiType, PropertyInfo[] contractProperties)
            {
                _apiType = apiType;
                _contractProperties = contractProperties;
            }

            public object Convert(object source)
            {
                var target = Activator.CreateInstance(_apiType)!;
                var targetType = target.GetType();

                foreach (var prop in _contractProperties)
                {
                    if (!prop.CanRead) continue;

                    var targetProp = targetType.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (targetProp != null && targetProp.CanWrite)
                    {
                        targetProp.SetValue(target, prop.GetValue(source));
                    }
                }

                return target;
            }
        }
    }
}
