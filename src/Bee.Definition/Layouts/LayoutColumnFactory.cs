using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Shared helper for converting <see cref="FormField"/> into layout-level fields/columns.
    /// Used by both <see cref="FormLayoutGenerator"/> and <see cref="ListLayoutGenerator"/>.
    /// </summary>
    internal static class LayoutColumnFactory
    {
        /// <summary>
        /// Builds a <see cref="LayoutField"/> for the master section from the given form field.
        /// </summary>
        public static LayoutField ToField(FormField field) => new()
        {
            FieldName = field.FieldName,
            Caption = field.Caption,
            ControlType = ResolveControlType(field.ControlType, field.DbType),
            DisplayFormat = field.DisplayFormat,
            NumberFormat = field.NumberFormat,
        };

        /// <summary>
        /// Builds a <see cref="LayoutColumn"/> for a grid from the given form field.
        /// </summary>
        public static LayoutColumn ToColumn(FormField field) => new()
        {
            FieldName = field.FieldName,
            Caption = field.Caption,
            ControlType = ResolveControlType(field.ControlType, field.DbType),
            Width = field.Width,
            DisplayFormat = field.DisplayFormat,
            NumberFormat = field.NumberFormat,
        };

        /// <summary>
        /// Resolves the effective control type, deriving a default from <paramref name="dbType"/>
        /// when <paramref name="type"/> is <see cref="ControlType.Auto"/>.
        /// </summary>
        public static ControlType ResolveControlType(ControlType type, FieldDbType dbType)
            => type != ControlType.Auto ? type : dbType switch
            {
                FieldDbType.Boolean => ControlType.CheckEdit,
                FieldDbType.DateTime => ControlType.DateEdit,
                FieldDbType.Text => ControlType.MemoEdit,
                _ => ControlType.TextEdit,
            };
    }
}
