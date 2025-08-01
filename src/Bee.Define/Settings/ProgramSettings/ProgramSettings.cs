using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 程式清單。
    /// </summary>
    [Serializable]
    [XmlType("ProgramSettings")]
    [Description("程式清單。")]
    [TreeNode("程式清單")]
    public class ProgramSettings : IObjectSerializeFile
    {
        private ProgramCategoryCollection _Categories = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ProgramSettings()
        {
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_Categories, serializeState);
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// 設定序列化/反序列化的對應檔案。
        /// </summary>
        /// <param name="fileName">檔案名稱。</param>
        public void SetObjectFilePath(string fileName)
        {
            ObjectFilePath = fileName;
        }

        #endregion

        /// <summary>
        /// 程式分類集合。
        /// </summary>
        [Description("資料表分類集合。")]
        [DefaultValue(null)]
        public ProgramCategoryCollection Categories
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Categories)) { return null; }
                if (_Categories == null) { _Categories = new ProgramCategoryCollection(this); }
                return _Categories;
            }
        }
    }
}
