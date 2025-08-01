﻿using System;
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
        /// 取得用戶輸入服務端點的類型。
        /// </summary>
        /// <param name="input">用戶輸入的服務端點。</param>
        public static EndpointType GetEndpointType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return EndpointType.Invalid;
            }

            // 判斷是否為網址
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                return EndpointType.Url;
            }

            // 判斷是否為本機路徑
            if (FileFunc.DirectoryExists(input))
            {
                return EndpointType.LocalPath;
            }

            // 判斷是否為網芳路徑（網路路徑）
            if (Regex.IsMatch(input, @"^\\\\[a-zA-Z0-9_\.]+\\[a-zA-Z0-9_\.\\]+$"))
            {
                return EndpointType.NetworkPath;
            }

            return EndpointType.Invalid;
        }

        /// <summary>
        /// 取得定義型別。
        /// </summary>
        /// <param name="defineType">定資資料類別。</param>
        /// <exception cref="NotSupportedException"></exception>
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
            LayoutColumn oColumn;
            ColumnControlType oControlType;

            oControlType = DefineFunc.ToColumnControlType(field.ControlType);
            oColumn = new LayoutColumn(field.FieldName, field.Caption, oControlType);
            if (field.Width > 0)
                oColumn.Width = field.Width;
            else
                oColumn.Width = 120;
            oColumn.DisplayFormat = field.DisplayFormat;
            oColumn.NumberFormat = field.NumberFormat;
            return oColumn;
        }

        /// <summary>
        /// 取得清單版面。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        internal static LayoutGrid GetListLayout(FormDefine formDefine)
        {
            FormTable oTable;
            FormField oField;
            LayoutGrid oGrid;
            LayoutColumn oColumn;
            string[] oFieldNames;

            oTable = formDefine.MasterTable;
            oFieldNames = StrFunc.Split(formDefine.ListFields, ",");

            oGrid = new LayoutGrid();
            oGrid.TableName = formDefine.ProgId;
            // 加入 sys_RowID 隱藏欄位
            oGrid.Columns.Add(SysFields.RowId, "列識別", ColumnControlType.TextEdit).Visible = false;
            // 加入清單顯示欄位
            foreach (string fieldName in oFieldNames)
            {
                oField = oTable.Fields[fieldName];
                if (oField != null)
                {
                    oColumn = ToLayoutColumn(oField);
                    oGrid.Columns.Add(oColumn);
                }
            }
            return oGrid;
        }
    }
}
