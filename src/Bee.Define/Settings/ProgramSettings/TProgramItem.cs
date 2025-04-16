using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 程式項目。
    /// </summary>
    [Serializable]
    [XmlType("ProgramItem")]
    [Description("程式項目。")]
    [TreeNode]
    public class TProgramItem : TKeyCollectionItem
    {
        private string _DisplayName = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TProgramItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public TProgramItem(string progID, string displayName)
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
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Description("顯示名稱。")]
        public string DisplayName
        {
            get { return this._DisplayName; }
            set { this._DisplayName = value; }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.ProgID} - {this.DisplayName}";
        }
    }
}
