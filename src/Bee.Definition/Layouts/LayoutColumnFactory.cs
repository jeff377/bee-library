using Bee.Base;
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
            ControlType = ResolveControlType(field),
            DisplayField = ResolveDisplayField(field),
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
            ControlType = ResolveControlType(field),
            DisplayField = ResolveDisplayField(field),
            Width = field.Width,
            DisplayFormat = field.DisplayFormat,
            NumberFormat = field.NumberFormat,
        };

        /// <summary>
        /// Resolves the effective control type for the given form field. An explicit
        /// <see cref="FormField.ControlType"/> wins; <see cref="ControlType.Auto"/> resolves
        /// to <see cref="ControlType.ButtonEdit"/> for relation fields (lookup editor),
        /// then falls back to the <see cref="FormField.DbType"/> mapping.
        /// </summary>
        public static ControlType ResolveControlType(FormField field)
            => field.ControlType == ControlType.Auto && StringUtilities.IsNotEmpty(field.RelationProgId)
                ? ControlType.ButtonEdit
                : ResolveControlType(field.ControlType, field.DbType);

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

        /// <summary>
        /// Resolves the display field for the given form field. An explicit
        /// <see cref="FormField.DisplayField"/> wins; relation fields fall back to the
        /// <see cref="FormField.RelationFieldMappings"/> entry whose source field is
        /// <c>sys_name</c>. Returns an empty string when no display field applies.
        /// </summary>
        public static string ResolveDisplayField(FormField field)
        {
            if (StringUtilities.IsNotEmpty(field.DisplayField))
                return field.DisplayField;
            if (StringUtilities.IsEmpty(field.RelationProgId))
                return string.Empty;
            var mapping = field.RelationFieldMappings?.FirstOrDefault(
                m => StringUtilities.IsEquals(m.SourceField, SysFields.Name));
            return mapping?.DestinationField ?? string.Empty;
        }
    }
}
