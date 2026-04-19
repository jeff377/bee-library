using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Base;

namespace Bee.Definition
{
    /// <summary>
    /// Utility library for define-related functions.
    /// </summary>
    public static class DefineFunc
    {
        private static readonly Dictionary<DefineType, string> DefineTypeNames = new Dictionary<DefineType, string>
        {
            { DefineType.SystemSettings,   "Bee.Definition.Settings.SystemSettings" },
            { DefineType.DatabaseSettings, "Bee.Definition.Settings.DatabaseSettings" },
            { DefineType.DbSchemaSettings, "Bee.Definition.Settings.DbSchemaSettings" },
            { DefineType.ProgramSettings,  "Bee.Definition.Settings.ProgramSettings" },
            { DefineType.TableSchema,      "Bee.Definition.Database.TableSchema" },
            { DefineType.FormSchema,       "Bee.Definition.Forms.FormSchema" },
            { DefineType.FormLayout,       "Bee.Definition.Layouts.FormLayout" },
        };

        /// <summary>
        /// Gets the CLR type for the specified define type.
        /// </summary>
        /// <param name="defineType">The define data type.</param>
        public static Type GetDefineType(DefineType defineType)
        {
            if (!DefineTypeNames.TryGetValue(defineType, out string? typeName))
                throw new NotSupportedException($"Type not found: {defineType}");
            // Get the current assembly
            var assembly = typeof(DefineFunc).Assembly;
            // Attempt to get the type
            var type = assembly.GetType(typeName);
            if (type == null)
                throw new NotSupportedException($"Type not found: {typeName}");
            return type;
        }

        /// <summary>
        /// Gets the number format string for the specified format name.
        /// </summary>
        /// <param name="numberFormat">The number format name.</param>
        public static string GetNumberFormatString(string numberFormat)
        {
            if (StrFunc.IsEmpty(numberFormat))
                return string.Empty;
            else if (StrFunc.IsEquals(numberFormat, "Quantity"))  // Quantity
                return "N0";
            else if (StrFunc.IsEquals(numberFormat, "UnitPrice"))  // Unit price
                return "N2";
            else if (StrFunc.IsEquals(numberFormat, "Amount"))  // Amount
                return "N2";
            else if (StrFunc.IsEquals(numberFormat, "Cost"))  // Cost
                return "N4";
            else
                return string.Empty;
        }

        /// <summary>
        /// Converts a <see cref="ControlType"/> to a <see cref="ColumnControlType"/>.
        /// </summary>
        /// <param name="type">The control type.</param>
        internal static ColumnControlType ToColumnControlType(ControlType type)
        {
            switch (type)
            {
                case ControlType.TextEdit:
                    return ColumnControlType.TextEdit;
                case ControlType.ButtonEdit:
                    return ColumnControlType.ButtonEdit;
                case ControlType.DateEdit:
                    return ColumnControlType.DateEdit;
                case ControlType.YearMonthEdit:
                    return ColumnControlType.YearMonthEdit;
                case ControlType.DropDownEdit:
                    return ColumnControlType.DropDownEdit;
                case ControlType.CheckEdit:
                    return ColumnControlType.CheckEdit;
                default:
                    return ColumnControlType.TextEdit;
            }
        }

        /// <summary>
        /// Converts a form field to a grid layout column.
        /// </summary>
        /// <param name="field">The form field.</param>
        internal static LayoutColumn ToLayoutColumn(FormField field)
        {
            var controlType = DefineFunc.ToColumnControlType(field.ControlType);
            var column = new LayoutColumn(field.FieldName, field.Caption, controlType);
            if (field.Width > 0)
                column.Width = field.Width;
            else
                column.Width = 120;
            column.DisplayFormat = field.DisplayFormat;
            column.NumberFormat = field.NumberFormat;
            return column;
        }

        /// <summary>
        /// Gets the list layout for the specified form schema.
        /// </summary>
        /// <param name="formDefine">The form schema definition.</param>
        internal static LayoutGrid GetListLayout(FormSchema formDefine)
        {
            var table = formDefine.MasterTable;
            string[] fieldNames = StrFunc.Split(formDefine.ListFields, ",");

            var grid = new LayoutGrid();
            grid.TableName = formDefine.ProgId;
            // Add sys_RowID hidden column
            grid.Columns!.Add(SysFields.RowId, "Row ID", ColumnControlType.TextEdit).Visible = false;
            // Add list display columns
            foreach (string fieldName in fieldNames)
            {
                var field = table!.Fields![fieldName];
                if (field != null)
                {
                    var column = ToLayoutColumn(field);
                    grid.Columns.Add(column);
                }
            }
            return grid;
        }


    }
}
