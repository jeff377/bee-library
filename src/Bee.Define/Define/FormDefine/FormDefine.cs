using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 表單定義。
    /// </summary>
    [Serializable]
    [XmlType("FormDefine")]
    [Description("表單定義。")]
    [TreeNode("表單定義")]
    public class FormDefine : IObjectSerializeFile
    {
        private string _ObjectFilePath = string.Empty;
        private SerializeState _SerializeState = SerializeState.None;
        private string _DisplayName = string.Empty;
        private FormTableCollection _Tables = null;
        private string _ListFields = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FormDefine()
        {
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState
        {
            get { return _SerializeState; }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            _SerializeState = serializeState;
            BaseFunc.SetSerializeState(_Tables, serializeState);
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath
        {
            get { return _ObjectFilePath; }
        }

        /// <summary>
        /// 設定序列化繫結檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public void SetObjectFilePath(string filePath)
        {
            _ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// 物件建立時間。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// 程式代碼。
        /// </summary>
        [XmlAttribute()]
        [Description("程式代碼。")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("顯示名稱。")]
        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        /// <summary>
        /// 清單欄位集合字串，以逗點分隔多個欄位。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("清單欄位集合字串，以逗點分隔多個欄位。")]
        public string ListFields
        {
            get { return _ListFields; }
            set { _ListFields = value; }
        }

        /// <summary>
        /// 資料表集合。
        /// </summary>
        [Description("資料表集合。")]
        [DefaultValue(null)]
        public FormTableCollection Tables
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Tables)) { return null; }
                if (_Tables == null) { _Tables = new FormTableCollection(this); }
                return _Tables;
            }
        }

        /// <summary>
        /// 主檔資料表。
        /// </summary>
        [Browsable(false)]
        [TreeNodeIgnore]
        public FormTable MasterTable
        {
            get
            {
                if (StrFunc.IsEmpty(this.ProgId) || !this.Tables.Contains(this.ProgId))
                    return null;
                else
                    return this.Tables[this.ProgId];
            }
        }

        /// <summary>
        /// 取得清單版面。
        /// </summary>
        public LayoutGrid GetListLayout()
        {
            return DefineFunc.GetListLayout(this);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.ProgId} - {this.DisplayName}";
        }
    }
}
