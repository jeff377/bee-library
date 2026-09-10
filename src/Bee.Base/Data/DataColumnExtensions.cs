using System.Data;

namespace Bee.Base.Data
{
    /// <summary>
    /// Extension methods for <see cref="DataColumn"/> that carry the declared
    /// <see cref="FieldDbType"/> of a column alongside its CLR type.
    /// </summary>
    /// <remarks>
    /// Several <see cref="FieldDbType"/> values share a single CLR type, so the declared field type
    /// cannot be recovered from <see cref="DataColumn.DataType"/> alone. The most consequential pair is
    /// <see cref="FieldDbType.Date"/> versus <see cref="FieldDbType.DateTime"/>, which both map to
    /// <see cref="DateTime"/> and so lose the calendar-day-versus-instant distinction that the
    /// definition layer went to the trouble of expressing.
    /// <para>
    /// The declared type is therefore recorded in <see cref="DataColumn.ExtendedProperties"/> whenever
    /// the framework knows it, and <see cref="ResolveFieldDbType"/> prefers that record over inferring
    /// from the CLR type. Serializers on both wire formats call these two methods so the mapping
    /// decision lives in one place.
    /// </para>
    /// </remarks>
    public static class DataColumnExtensions
    {
        /// <summary>
        /// The <see cref="DataColumn.ExtendedProperties"/> key under which the declared field type is stored.
        /// </summary>
        private const string FieldDbTypeKey = "Bee.FieldDbType";

        /// <summary>
        /// Records the declared <see cref="FieldDbType"/> of the column.
        /// </summary>
        /// <param name="column">The target column.</param>
        /// <param name="dbType">The declared field database type.</param>
        public static void ApplyFieldDbType(this DataColumn column, FieldDbType dbType)
        {
            ArgumentNullException.ThrowIfNull(column);
            column.ExtendedProperties[FieldDbTypeKey] = dbType;
        }

        /// <summary>
        /// Gets the declared <see cref="FieldDbType"/> recorded by <see cref="ApplyFieldDbType"/>,
        /// or <see langword="null"/> when the column carries no record.
        /// </summary>
        /// <param name="column">The target column.</param>
        /// <remarks>
        /// The record is normally a <see cref="FieldDbType"/> value. A column restored by
        /// <see cref="DataSet.ReadXml(System.Xml.XmlReader, XmlReadMode)"/> carries it as the member name
        /// in string form instead, because the XSD annotation is written and read back as text, so that
        /// form is accepted too. Any other value, including a string that is not exactly a member name,
        /// is treated as no record.
        /// </remarks>
        public static FieldDbType? GetDeclaredFieldDbType(this DataColumn column)
        {
            ArgumentNullException.ThrowIfNull(column);
            return column.ExtendedProperties[FieldDbTypeKey] switch
            {
                FieldDbType dbType => dbType,
                string name => ParseMemberName(name),
                _ => null,
            };
        }

        private static FieldDbType? ParseMemberName(string name)
        {
            // `Enum.TryParse` also accepts numeric strings, comma-separated lists and surrounding
            // whitespace. The framework writes the annotation as the member name, so a value that does
            // not name a defined member and spell it back identically is not a record it wrote.
            return Enum.TryParse(name, out FieldDbType parsed)
                && Enum.IsDefined(parsed)
                && string.Equals(parsed.ToString(), name, StringComparison.Ordinal)
                ? parsed
                : null;
        }

        /// <summary>
        /// Gets the <see cref="FieldDbType"/> of the column, preferring the declared type recorded by
        /// <see cref="ApplyFieldDbType"/> and falling back to inferring it from
        /// <see cref="DataColumn.DataType"/>.
        /// </summary>
        /// <param name="column">The target column.</param>
        /// <exception cref="InvalidOperationException">
        /// The column carries no declared type and its CLR type has no <see cref="FieldDbType"/> equivalent.
        /// </exception>
        public static FieldDbType ResolveFieldDbType(this DataColumn column)
        {
            return column.GetDeclaredFieldDbType() ?? DbTypeConverter.ToFieldDbType(column.DataType);
        }
    }
}
