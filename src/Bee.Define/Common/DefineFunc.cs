using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 定義相關函式庫。
    /// </summary>
    public static class DefineFunc
    {
        /// <summary>
        /// 取得定義型別。
        /// </summary>
        /// <param name="defineType">定資資料類別。</param>
        public static Type GetDefineType(DefineType defineType)
        {
            // 取得型別名稱
            string typeName = "Bee.Define." + defineType.ToString();
            // 取得目前組件
            var assembly = typeof(DefineFunc).Assembly;
            // 嘗試取得型別
            var type = assembly.GetType(typeName);
            if (type == null)
                throw new NotSupportedException($"Type not found: {typeName}");
            return type;
        }

        /// <summary>
        /// 取得數值格式化字串。
        /// </summary>
        /// <param name="numberFormat">數值格式化。</param>
        public static string GetNumberFormatString(string numberFormat)
        {
            if (StrFunc.IsEmpty(numberFormat))
                return string.Empty;
            else if (StrFunc.IsEquals(numberFormat, "Quantity"))  // 數量
                return "N0";
            else if (StrFunc.IsEquals(numberFormat, "UnitPrice"))  // 單價
                return "N2";
            else if (StrFunc.IsEquals(numberFormat, "Amount"))  // 金額
                return "N2";
            else if (StrFunc.IsEquals(numberFormat, "Cost"))  // 成本
                return "N4";
            else
                return string.Empty;
        }

        /// <summary>
        /// 轉換為表格欄位的控制項類型。
        /// </summary>
        /// <param name="type">控制項類型。</param>
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
        /// 轉換為資料表格排版欄位。
        /// </summary>
        /// <param name="field">表單欄位。</param>
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
        /// 取得清單版面。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        internal static LayoutGrid GetListLayout(FormDefine formDefine)
        {
            var table = formDefine.MasterTable;
            string[] fieldNames = StrFunc.Split(formDefine.ListFields, ",");

            var grid = new LayoutGrid();
            grid.TableName = formDefine.ProgId;
            // 加入 sys_RowID 隱藏欄位
            grid.Columns.Add(SysFields.RowId, "列識別", ColumnControlType.TextEdit).Visible = false;
            // 加入清單顯示欄位
            foreach (string fieldName in fieldNames)
            {
                var field = table.Fields[fieldName];
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
