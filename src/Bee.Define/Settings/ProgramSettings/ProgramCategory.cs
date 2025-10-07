using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 程式分類。
    /// </summary>
    [Serializable]
    [XmlType("ProgramCategory")]
    [Description("程式分類。")]
    [TreeNode]
    public class ProgramCategory : KeyCollectionItem
    {
        private ProgramItemCollection _items = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ProgramCategory()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="id">分類代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public ProgramCategory(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 分類代碼。
        /// </summary>
        [XmlAttribute]
        [Description("分類代碼。")]
        public string Id
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Description("顯示名稱。")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 程式項目集合。
        /// </summary>
        [Description("程式項目集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public ProgramItemCollection Items
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _items)) { return null; }
                if (_items == null) { _items = new ProgramItemCollection(this); }
                return _items;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_items, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.Id} - {this.DisplayName}";
        }
    }
}
