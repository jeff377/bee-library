using System;
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
        public static EEndpointType GetEndpointType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return EEndpointType.Invalid;
            }

            // 判斷是否為網址
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                return EEndpointType.Url;
            }

            // 判斷是否為本機路徑
            if (FileFunc.DirectoryExists(input))
            {
                return EEndpointType.LocalPath;
            }

            // 判斷是否為網芳路徑（網路路徑）
            if (Regex.IsMatch(input, @"^\\\\[a-zA-Z0-9_\.]+\\[a-zA-Z0-9_\.\\]+$"))
            {
                return EEndpointType.NetworkPath;
            }

            return EEndpointType.Invalid;
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
        internal static EColumnControlType ToColumnControlType(EControlType type)
        {
            switch (type)
            {
                case EControlType.TextEdit:
                    return EColumnControlType.TextEdit;
                case EControlType.ButtonEdit:
                    return EColumnControlType.ButtonEdit;
                case EControlType.DateEdit:
                    return EColumnControlType.DateEdit;
                case EControlType.YearMonthEdit:
                    return EColumnControlType.YearMonthEdit;
                case EControlType.DropDownEdit:
                    return EColumnControlType.DropDownEdit;
                case EControlType.CheckEdit:
                    return EColumnControlType.CheckEdit;
                default:
                    return EColumnControlType.TextEdit;
            }
        }

        /// <summary>
        /// 轉換為資料表格排版欄位。
        /// </summary>
        /// <param name="field">表單欄位。</param>
        internal static TLayoutColumn ToLayoutColumn(TFormField field)
        {
            TLayoutColumn oColumn;
            EColumnControlType oControlType;

            oControlType = DefineFunc.ToColumnControlType(field.ControlType);
            oColumn = new TLayoutColumn(field.FieldName, field.Caption, oControlType);
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
        internal static TLayoutGrid GetListLayout(TFormDefine formDefine)
        {
            TFormTable oTable;
            TFormField oField;
            TLayoutGrid oGrid;
            TLayoutColumn oColumn;
            string[] oFieldNames;

            oTable = formDefine.MasterTable;
            oFieldNames = StrFunc.Split(formDefine.ListFields, ",");

            oGrid = new TLayoutGrid();
            oGrid.TableName = formDefine.ProgID;
            // 加入 sys_RowID 隱藏欄位
            oGrid.Columns.Add(SysFields.RowId, "列識別", EColumnControlType.TextEdit).Visible = false;
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
