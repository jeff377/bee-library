using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料表格排版。
    /// </summary>
    [Serializable]
    [XmlType("LayoutGrid")]
    [Description("資料表格排版。")]
    [TreeNode]
    public class LayoutGrid : LayoutItemBase
    {
        private string _TableName = string.Empty;
        private string _DisplayName = string.Empty;
        private GridControlAllowActions _AllowActions = GridControlAllowActions.All;
        private LayoutColumnCollection _Columns = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public LayoutGrid()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="displayName">顯示名稱。</param>
        public LayoutGrid(string tableName, string displayName)
        {
            _TableName = tableName;
            _DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("資料表名稱。")]
        public string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("顯示名稱。")]
        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        /// <summary>
        /// Grid 控制項允許執行的動作。
        /// </summary>
        [XmlAttribute]
        [Description("Grid 控制項允許執行的動作。")]
        [DefaultValue(GridControlAllowActions.All)]
        public GridControlAllowActions AllowActions
        {
            get { return _AllowActions; }
            set { _AllowActions = value; }
        }

        /// <summary>
        /// 欄位集合。
        /// </summary>
        [Description("欄位集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public LayoutColumnCollection Columns
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Columns)) { return null; }
                if (_Columns == null) { _Columns = new LayoutColumnCollection(); }
                return _Columns;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_Columns, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.TableName} - {this.DisplayName}";
        }
    }
}
