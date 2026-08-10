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
            SerializationAttributeNames.CollectionBase,
            SerializationAttributeNames.KeyCollectionBase,
            SerializationAttributeNames.KeyCollectionBase,
            SerializationAttributeNames.CollectionBase,
        };

        private static readonly string[] s_itemMetadataNames =
        {
            SerializationAttributeNames.CollectionItem,
            SerializationAttributeNames.KeyCollectionItem,
        };

        private readonly ImmutableArray<INamedTypeSymbol> _baseTypes;
        private readonly ImmutableArray<INamedTypeSymbol> _itemBaseTypes;

        private FrameworkCollectionTypes(
            ImmutableArray<INamedTypeSymbol> baseTypes, ImmutableArray<INamedTypeSymbol> itemBaseTypes)
        {
            _baseTypes = baseTypes;
            _itemBaseTypes = itemBaseTypes;
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

            var itemBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (var name in s_itemMetadataNames)
            {
                var symbol = compilation.GetTypeByMetadataName(name);
                if (symbol is not null)
                    itemBuilder.Add(symbol);
            }

            return builder.Count > 0 || itemBuilder.Count > 0
                ? new FrameworkCollectionTypes(builder.ToImmutable(), itemBuilder.ToImmutable())
                : null;
        }

        /// <summary>
        /// Determines whether the specified type is itself one of the framework collection bases.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><c>true</c> when the type is a collection base rather than a user of one.</returns>
        /// <remarks>
        /// The bases carry serialization plumbing of their own — a proxy property that flattens the
        /// collection for the wire, for instance — which follows different rules from the definition
        /// types that consume them.
        /// </remarks>
        public bool IsCollectionBase(INamedTypeSymbol type)
        {
            return _baseTypes
                .Any(baseType => SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, baseType));
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
                if (_baseTypes.Any(baseType => SymbolEqualityComparer.Default.Equals(definition, baseType)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified type derives from one of the framework collection item
        /// bases.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><c>true</c> when any base type is a framework collection item.</returns>
        /// <remarks>
        /// Items are constructed by the deserializer just as their collections are, so the shape
        /// rules that apply to one apply to the other. Before the wire attributes were removed this
        /// was covered incidentally — items carried <c>[MessagePackObject]</c> and the rules keyed
        /// off that; the check has to be explicit now.
        /// </remarks>
        public bool IsFrameworkCollectionItem(INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (_itemBaseTypes.Any(b => SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, b)))
                    return true;
            }

            return false;
        }
    }
}
