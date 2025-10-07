using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 表單版面配置。
    /// </summary>
    [Serializable]
    [XmlType("FormLayout")]
    [Description("表單版面配置。")]
    [TreeNode]
    public class FormLayout : IObjectSerializeFile
    {
        private LayoutGroupCollection _groups = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FormLayout()
        {
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_groups, serializeState);
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// 設定序列化繫結檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// 物件建立時間。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// 表單版面代碼。
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("表單版面代碼。")]
        public string LayoutId { get; set; } = string.Empty;

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("顯示名稱。")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 佈局群組集合。
        /// </summary>
        [Description("佈局群組集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public LayoutGroupCollection Groups
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _groups)) { return null; }
                if (_groups == null) { _groups = new LayoutGroupCollection(); }
                return _groups;
            }
        }

        /// <summary>
        /// 尋找指定欄位名稱的排版項目。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        public LayoutItem FindItem(string fieldName)
        {
            foreach (LayoutGroup group in this.Groups)
            {
                foreach (LayoutItem item in group.Items)
                {
                    if (StrFunc.IsEquals(item.FieldName, fieldName))
                        return item;
                }
            }
            return null;
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.LayoutId} - {this.DisplayName}";
        }
    }
}
