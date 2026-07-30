using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reads the MessagePack contract options a type declares.
    /// </summary>
    internal static class MessagePackContract
    {
        private const string KeyAsPropertyNameArgument = "keyAsPropertyName";

        /// <summary>
        /// Determines whether the contract opts the type into name-based keys.
        /// </summary>
        /// <param name="attribute">The <c>MessagePackObject</c> attribute applied to a type.</param>
        /// <returns><c>true</c> when <c>keyAsPropertyName</c> is set.</returns>
        /// <remarks>
        /// The option is checked positionally as well as by name because the attribute declares it as an
        /// optional constructor parameter: writing <c>[MessagePackObject]</c> still yields one
        /// constructor argument, whose value is <c>false</c>.
        /// </remarks>
        public static bool UsesNameBasedKeys(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is bool positional)
                return positional;

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == KeyAsPropertyNameArgument && argument.Value.Value is bool named)
                    return named;
            }

            return false;
        }
    }
}
