using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Resolves the framework collection base types and tests whether a type derives from one.
    /// </summary>
    internal sealed class FrameworkCollectionTypes
    {
        private static readonly string[] s_metadataNames =
        {
            SerializationAttributeNames.MessagePackCollectionBase,
            SerializationAttributeNames.MessagePackKeyCollectionBase,
            SerializationAttributeNames.KeyCollectionBase,
            SerializationAttributeNames.CollectionBase,
        };

        private readonly ImmutableArray<INamedTypeSymbol> _baseTypes;

        private FrameworkCollectionTypes(ImmutableArray<INamedTypeSymbol> baseTypes)
        {
            _baseTypes = baseTypes;
        }

        /// <summary>
        /// Resolves the collection base types available to the specified compilation.
        /// </summary>
        /// <param name="compilation">The compilation to resolve against.</param>
        /// <returns>
        /// The resolved types, or <c>null</c> when the compilation references none of them — a project
        /// that does not use the framework collections has nothing for these rules to check.
        /// </returns>
        public static FrameworkCollectionTypes? TryResolve(Compilation compilation)
        {
            var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

            foreach (var name in s_metadataNames)
            {
                var symbol = compilation.GetTypeByMetadataName(name);
                if (symbol is not null)
                    builder.Add(symbol);
            }

            return builder.Count > 0 ? new FrameworkCollectionTypes(builder.ToImmutable()) : null;
        }

        /// <summary>
        /// Determines whether the specified type derives from one of the framework collection bases.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><c>true</c> when any base type is a constructed framework collection.</returns>
        public bool IsFrameworkCollection(INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (!current.IsGenericType)
                    continue;

                var definition = current.OriginalDefinition;
                foreach (var baseType in _baseTypes)
                {
                    if (SymbolEqualityComparer.Default.Equals(definition, baseType))
                        return true;
                }
            }

            return false;
        }
    }
}
