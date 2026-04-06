using Bee.Define.Forms;
using Bee.Base;
using Bee.Base.Data;
using System;

namespace Bee.Define.Layouts
{
    /// <summary>
    /// 表單版面配置生成器。
    /// 負責將 FormSchema 轉換為 FormLayout 結構。
    /// </summary>
    public class FormLayoutGenerator
    {
        /// <summary>
        /// 生成表單版面配置。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        /// <returns>表單版面配置。</returns>
        public FormLayout Generate(FormSchema formDefine)
        {
            if (formDefine == null)
                throw new ArgumentNullException(nameof(formDefine));

            var formLayout = new FormLayout
            {
                LayoutId = formDefine.ProgId,
                DisplayName = formDefine.DisplayName
            };

            AddLayoutGroups(formDefine, formLayout);

            return formLayout;
        }

        /// <summary>
        /// 加入版面配置群組。
        /// </summary>
        private void AddLayoutGroups(FormSchema formDefine, FormLayout formLayout)
        {
            if (formDefine.Tables == null) return;

            // 為主表建立版面配置群組
            if (formDefine.MasterTable != null)
            {
                AddMasterTableGroup(formDefine.MasterTable, formLayout);
            }

            // 為其他資料表建立版面配置群組
            foreach (var table in formDefine.Tables)
            {
                // 跳過主表（已處理）
                if (table == formDefine.MasterTable)
                    continue;

                AddDetailTableGroup(table, formLayout);
            }
        }

        /// <summary>
        /// 加入主表版面配置群組。
        /// </summary>
        private void AddMasterTableGroup(FormTable formTable, FormLayout formLayout)
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

                // 設定關連程式代碼
                if (StrFunc.IsNotEmpty(field.LookupProgId))
                {
                    layoutItem.ProgId = field.LookupProgId;
                }
                else if (StrFunc.IsNotEmpty(field.RelationProgId))
                {
                    layoutItem.ProgId = field.RelationProgId;
                }

                group.Items.Add(layoutItem);
            }

            if (group.Items.Count > 0)
            {
                formLayout.Groups.Add(group);
            }
        }

        /// <summary>
        /// 加入明細表版面配置群組。
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

            // 建立資料表格排版
            var layoutGrid = new LayoutGrid(formTable.TableName, formTable.DisplayName);

            // 加入欄位
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

                layoutGrid.Columns.Add(column);
            }

            if (layoutGrid.Columns.Count > 0)
            {
                group.Items.Add(layoutGrid);
                formLayout.Groups.Add(group);
            }
        }

        /// <summary>
        /// 根據資料型別取得預設控制項類型。
        /// </summary>
        private ControlType GetDefaultControlType(FieldDbType dbType)
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
        /// 將 ControlType 轉換為 ColumnControlType。
        /// </summary>
        private ColumnControlType ConvertToColumnControlType(ControlType controlType)
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
                    // MemoEdit 在表格欄位中不適用，轉換為 TextEdit
                    return ColumnControlType.TextEdit;
                case ControlType.Auto:
                default:
                    return ColumnControlType.Auto;
            }
        }

        /// <summary>
        /// 根據資料型別取得預設表格欄位控制項類型。
        /// </summary>
        private ColumnControlType GetDefaultColumnControlType(FieldDbType dbType)
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