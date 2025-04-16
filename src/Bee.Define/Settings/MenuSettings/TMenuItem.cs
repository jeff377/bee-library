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
    public class TMenuItem : TKeyCollectionItem, IDisplayName
    {
        private string _DisplayName = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TMenuItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public TMenuItem(string progID, string displayName)
        {
            this.ProgID = progID;
            _DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        [XmlAttribute]
        [Description("程式代碼。")]
        public string ProgID
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Description("顯示名稱。")]
        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        /// <summary>
        /// 產生物件複本。
        /// </summary>
        /// <returns></returns>
        public TMenuItem Clone()
        {
            TMenuItem oItem;

            oItem = new TMenuItem();
            oItem.ProgID = this.ProgID;
            oItem.DisplayName = this.DisplayName;
            return oItem;
        }

        /// <summary>
        /// 物件的描述文字。
        /// </summary>
        public override string ToString()
        {
            return StrFunc.Format("{0} - {1}", this.ProgID, this.DisplayName);
        }
    }
}
