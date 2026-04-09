using Bee.Base;
using Bee.Base.Data;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Interface for a define field.
    /// </summary>
    public interface IDefineField
    {
        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        string FieldName { get; set; }

        /// <summary>
        /// Gets or sets the caption text.
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        /// Gets or sets the database data type.
        /// </summary>
        FieldDbType DbType { get; set; }
    }
}