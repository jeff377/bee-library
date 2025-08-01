using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 選單項目。
    /// </summary>
    [Serializable]
    [XmlType("MenuItem")]
    [Description("選單項目。")]
    [TreeNode]
    public class MenuItem : KeyCollectionItem, IDisplayName
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public MenuItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public MenuItem(string progId, string displayName)
        {
            ProgId = progId;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        [XmlAttribute]
        [Description("程式代碼。")]
        public string ProgId
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Description("顯示名稱。")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 產生物件複本。
        /// </summary>
        /// <returns></returns>
        public MenuItem Clone()
        {
            var item = new MenuItem();
            item.ProgId = this.ProgId;
            item.DisplayName = this.DisplayName;
            return item;
        }

        /// <summary>
        /// 物件的描述文字。
        /// </summary>
        public override string ToString()
        {
            return StrFunc.Format("{0} - {1}", this.ProgId, this.DisplayName);
        }
    }
}
