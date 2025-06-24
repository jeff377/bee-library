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
    public class ProgramItem : KeyCollectionItem
    {
        private string _DisplayName = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ProgramItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public ProgramItem(string progId, string displayName)
        {
            this.ProgId = progId;
            _DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        [XmlAttribute]
        [Description("程式代碼。")]
        public string ProgId
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
            return $"{this.ProgId} - {this.DisplayName}";
        }
    }
}
