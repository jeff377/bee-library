using System.Text.Json;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Reader helpers shared by the <see cref="System.Data.DataTable"/> and
    /// <see cref="System.Data.DataSet"/> converters.
    /// </summary>
    /// <remarks>
    /// The two converters read the same JSON shapes and held verbatim copies of these helpers.
    /// They sit in the same folder and are edited together, which is exactly the arrangement where
    /// a copy survives unnoticed — one side gets a fix and the other keeps the old behaviour on a
    /// wire that is supposed to be one wire.
    /// </remarks>
    internal static class JsonReaderExtensions
    {
        /// <summary>
        /// Reads a JSON array of strings, skipping any element that is not a string.
        /// </summary>
        /// <param name="reader">The reader, positioned on the array's start token.</param>
        /// <returns>The strings read; empty when the current token is not an array.</returns>
        /// <remarks>
        /// A non-array token yields an empty list rather than throwing: these members are optional
        /// in the payload, and a missing one is not a malformed document.
        /// </remarks>
        public static List<string> ReadStringArray(ref Utf8JsonReader reader)
        {
            var list = new List<string>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                    list.Add(reader.GetString()!);
            }
            return list;
        }
    }
}
