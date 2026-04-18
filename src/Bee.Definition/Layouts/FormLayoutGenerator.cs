using Bee.Definition.Forms;
using Bee.Base;
using Bee.Base.Data;
using System;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Form layout generator.
    /// Responsible for converting a <see cref="FormSchema"/> into a <see cref="FormLayout"/> structure.
    /// </summary>
    public class FormLayoutGenerator
    {
        /// <summary>
        /// Generates a form layout from a form schema definition.
        /// </summary>
        /// <param name="formDefine">The form schema definition.</param>
        /// <returns>The generated form layout.</returns>
        public FormLayout Generate(FormSchema formDefine)
        {
            ArgumentNullException.ThrowIfNull(formDefine);

            var formLayout = new FormLayout
            {
                LayoutId = formDefine.ProgId,
                DisplayName = formDefine.DisplayName
            };

            AddLayoutGroups(formDefine, formLayout);

            return formLayout;
        }

        /// <summary>
        /// Adds layout groups to the form layout.
        /// </summary>
        private void AddLayoutGroups(FormSchema formDefine, FormLayout formLayout)
        {
            if (formDefine.Tables == null) return;

            // Create a layout group for the master table
            if (formDefine.MasterTable != null)
            {
                AddMasterTableGroup(formDefine.MasterTable, formLayout);
            }

            // Create layout groups for the remaining tables
            foreach (var table in formDefine.Tables)
            {
                // Skip the master table (already handled)
                if (table == formDefine.MasterTable)
                    continue;

                AddDetailTableGroup(table, formLayout);
            }
        }

        /// <summary>
        /// Adds a master table layout group.
        /// </summary>
        private static void AddMasterTableGroup(FormTable formTable, FormLayout formLayout)
        {
            if (formTable.Fields == null) return;

            var group = new LayoutGroup
            {
                Name = "MainGroup",
                Caption = formTable.DisplayName,
                ShowCaption = true,
                ColumnCount = 2
            };

            foreach (var field in formTable.Fields)
            {
                if (!field.Visible) continue;

                var layoutItem = new LayoutItem
                {
                    FieldName = field.FieldName,
                    Caption = field.Caption,
                    ControlType = field.ControlType == ControlType.Auto
                        ? GetDefaultControlType(field.DbType)
                        : field.ControlType,
                    DisplayFormat = field.DisplayFormat,
                    NumberFormat = field.NumberFormat
                };

                // Set the related program ID
                if (StrFunc.IsNotEmpty(field.LookupProgId))
                {
                    layoutItem.ProgId = field.LookupProgId;
                }
                else if (StrFunc.IsNotEmpty(field.RelationProgId))
                {
                    layoutItem.ProgId = field.RelationProgId;
                }

                group.Items!.Add(layoutItem);
            }

            if (group.Items!.Count > 0)
            {
                formLayout.Groups!.Add(group);
            }
        }

        /// <summary>
        /// Adds a detail table layout group.
        /// </summary>
        private void AddDetailTableGroup(FormTable formTable, FormLayout formLayout)
        {
            if (formTable.Fields == null) return;

            var group = new LayoutGroup
            {
                Name = formTable.TableName + "Group",
                Caption = formTable.DisplayName,
                ShowCaption = true,
                ColumnCount = 1
            };

            // Create a grid layout for the table
            var layoutGrid = new LayoutGrid(formTable.TableName, formTable.DisplayName);

            // Add columns
            foreach (var field in formTable.Fields)
            {
                if (!field.Visible) continue;

                var column = new LayoutColumn
                {
                    FieldName = field.FieldName,
                    Caption = field.Caption,
                    ControlType = field.ControlType == ControlType.Auto
                        ? GetDefaultColumnControlType(field.DbType)
                        : ConvertToColumnControlType(field.ControlType),
                    Width = field.Width > 0 ? field.Width : 100,
                    DisplayFormat = field.DisplayFormat,
                    NumberFormat = field.NumberFormat
                };

                layoutGrid.Columns!.Add(column);
            }

            if (layoutGrid.Columns!.Count > 0)
            {
                group.Items!.Add(layoutGrid);
                formLayout.Groups!.Add(group);
            }
        }

        /// <summary>
        /// Gets the default control type for the specified database field type.
        /// </summary>
        private static ControlType GetDefaultControlType(FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.Boolean:
                    return ControlType.CheckEdit;
                case FieldDbType.DateTime:
                    return ControlType.DateEdit;
                case FieldDbType.Text:
                    return ControlType.MemoEdit;
                default:
                    return ControlType.TextEdit;
            }
        }

        /// <summary>
        /// Converts a <see cref="ControlType"/> to a <see cref="ColumnControlType"/>.
        /// </summary>
        private static ColumnControlType ConvertToColumnControlType(ControlType controlType)
        {
            switch (controlType)
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
                case ControlType.MemoEdit:
                    // MemoEdit is not applicable in grid columns; fall back to TextEdit
                    return ColumnControlType.TextEdit;
                case ControlType.Auto:
                default:
                    return ColumnControlType.Auto;
            }
        }

        /// <summary>
        /// Gets the default grid column control type for the specified database field type.
        /// </summary>
        private static ColumnControlType GetDefaultColumnControlType(FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.Boolean:
                    return ColumnControlType.CheckEdit;
                case FieldDbType.DateTime:
                    return ColumnControlType.DateEdit;
                default:
                    return ColumnControlType.TextEdit;
            }
        }
    }
}
