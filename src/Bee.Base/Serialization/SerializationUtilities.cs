using System.Collections;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Helpers shared across serialization codecs (XML / JSON / MessagePack).
    /// </summary>
    public static class SerializationUtilities
    {
        /// <summary>
        /// Determines whether the value is empty during serialization; null and DBNull are both treated as empty.
        /// Returns <c>false</c> when <paramref name="serializeState"/> is not
        /// <see cref="SerializeState.Serialize"/>, so deserialization paths preserve every value.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        /// <param name="value">The value to check.</param>
        public static bool IsSerializeEmpty(SerializeState serializeState, object value)
        {
            if (serializeState != SerializeState.Serialize) { return false; }

            switch (value)
            {
                case null:
                    return true;
                case IObjectSerializeEmpty objectSerializeEmpty:
                    return objectSerializeEmpty.IsSerializeEmpty;
                case IList listValue:
                    return ValueUtilities.IsEmpty(listValue);
                case IEnumerable enumerableValue:
                    return ValueUtilities.IsEmpty(enumerableValue);
                default:
                    return false;
            }
        }
    }
}
