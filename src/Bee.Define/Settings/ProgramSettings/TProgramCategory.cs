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
    public class TProgramCategory : TKeyCollectionItem
    {
        private string _DisplayName = string.Empty;
        private TProgramItemCollection _Items = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TProgramCategory()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="id">分類代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public TProgramCategory(string id, string displayName)
        {
            this.ID = id;
            _DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 分類代碼。
        /// </summary>
        [XmlAttribute]
        [Description("分類代碼。")]
        public string ID
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
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        /// <summary>
        /// 程式項目集合。
        /// </summary>
        [Description("程式項目集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public TProgramItemCollection Items
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Items)) { return null; }
                if (_Items == null) { _Items = new TProgramItemCollection(this); }
                return _Items;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(ESerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_Items, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.ID} - {this.DisplayName}";
        }
    }
}
