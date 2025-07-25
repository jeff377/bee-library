﻿using System;
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
        private string _ObjectFilePath = string.Empty;
        private SerializeState _SerializeState = SerializeState.None;
        private DateTime _CreateTime = DateTime.MinValue;
        private string _DisplayName = string.Empty;
        private LayoutGroupCollection _Groups = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FormLayout()
        {
            _CreateTime = DateTime.Now;
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
            BaseFunc.SetSerializeState(_Groups, serializeState);
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
        [JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime
        {
            get { return _CreateTime; }
        }

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
        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

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
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Groups)) { return null; }
                if (_Groups == null) { _Groups = new LayoutGroupCollection(); }
                return _Groups;
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
