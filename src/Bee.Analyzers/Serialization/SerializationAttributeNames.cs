namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Metadata names of the serialization attributes and base types the wire contract rules resolve.
    /// </summary>
    /// <remarks>
    /// Resolved by metadata name rather than referenced directly: the analyzer targets netstandard2.0
    /// and cannot reference the net10.0 framework assemblies, and a consumer that uses neither
    /// MessagePack nor the framework collections should see no diagnostics at all rather than have the
    /// rules guess.
    /// </remarks>
    internal static class SerializationAttributeNames
    {
        /// <summary>
        /// The MessagePack contract attribute applied to serializable types.
        /// </summary>
        public const string MessagePackObjectAttribute = "MessagePack.MessagePackObjectAttribute";

        /// <summary>
        /// The MessagePack attribute assigning an integer or name-based key to a member.
        /// </summary>
        public const string KeyAttribute = "MessagePack.KeyAttribute";

        /// <summary>
        /// The MessagePack attribute excluding a member from serialization.
        /// </summary>
        public const string IgnoreMemberAttribute = "MessagePack.IgnoreMemberAttribute";

        /// <summary>
        /// The MessagePack attribute declaring a polymorphic subtype on a union base.
        /// </summary>
        public const string UnionAttribute = "MessagePack.UnionAttribute";

        /// <summary>
        /// The System.Text.Json attribute renaming a property on the JSON wire.
        /// </summary>
        public const string JsonPropertyNameAttribute = "System.Text.Json.Serialization.JsonPropertyNameAttribute";

        /// <summary>
        /// The System.Text.Json attribute excluding a property from serialization.
        /// </summary>
        public const string JsonIgnoreAttribute = "System.Text.Json.Serialization.JsonIgnoreAttribute";

        /// <summary>
        /// The XML attribute excluding a member from serialization.
        /// </summary>
        public const string XmlIgnoreAttribute = "System.Xml.Serialization.XmlIgnoreAttribute";

        /// <summary>
        /// The framework base type for keyed collections.
        /// </summary>
        public const string KeyCollectionBase = "Bee.Base.Collections.KeyCollectionBase`1";

        /// <summary>
        /// The framework base type for collections.
        /// </summary>
        public const string CollectionBase = "Bee.Base.Collections.CollectionBase`1";
    }
}
