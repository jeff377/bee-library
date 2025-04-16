using System;

namespace Bee.Base
{
    /// <summary>
    /// 套用於類別，描述物件繫結於樹狀節點呈現方式的自訂屬性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TreeNodeAttribute : Attribute
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TreeNodeAttribute()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="displayFormat">顯示名稱。</param>
        public TreeNodeAttribute(string displayFormat)
        {
            DisplayFormat = displayFormat;
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="displayFormat">顯示的格式化字串。</param>
        /// <param name="propertyName">取代格式化字串中 {0} 的屬性名稱。</param>
        public TreeNodeAttribute(string displayFormat, string propertyName)
        {
            DisplayFormat = displayFormat;
            PropertyName = propertyName;
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="displayFormat">顯示名稱。</param>
        /// <param name="collectionFolder">集合屬性是否顯示資料夾節點。</param>
        public TreeNodeAttribute(string displayFormat, bool collectionFolder)
        {
            DisplayFormat = displayFormat;
            CollectionFolder = collectionFolder;
        }

        #endregion

        /// <summary>
        /// 顯示的格式化字串。
        /// </summary>
        public string DisplayFormat { get; private set; } = string.Empty;

        /// <summary>
        /// 取代格式化字串中 {0} 的屬性名稱。
        /// </summary>
        public string PropertyName { get; private set; } = string.Empty ;

        /// <summary>
        /// 集合屬性是否顯示資料夾節點。
        /// </summary>
        public bool CollectionFolder { get; private set; } = false;

        /// <summary>
        /// 取得套用 TreeNodeAttribute 的顯示文字。
        /// </summary>
        /// <param name="value"></param>
        public static string GetDisplayText(object value)
        {
            // 取得元件的 TreeNodeAttribute
            var attribute = (TreeNodeAttribute)BaseFunc.GetAttribute(value, typeof(TreeNodeAttribute));
            // 若無 Attribute 則回傳物件描述文字
            if (attribute == null) { return value.ToString(); }

            string displayText;
            if (StrFunc.IsNotEmpty(attribute.PropertyName))
            {
                // DisplayFormat 為格式化字串
                var names = StrFunc.Split(attribute.PropertyName, ",");
                var args = new object[names.Length];
                for (int N1 = 0; N1 < names.Length; N1++)
                    args[N1] = BaseFunc.GetPropertyValue(value, names[N1]);
                displayText = StrFunc.Format(attribute.DisplayFormat, args);
            }
            else
            {
                // DisplayFormat 為指定字串
                displayText = attribute.DisplayFormat;
            }

            if (StrFunc.IsEmpty(displayText))
            {
                if (value is IDisplayName)
                    displayText = (value as IDisplayName).DisplayName;
                else
                    displayText = value.ToString();
            }

            return displayText;
        }

    }
}
