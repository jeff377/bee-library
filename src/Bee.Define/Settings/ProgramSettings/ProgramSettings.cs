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
        private string _ObjectFilePath = string.Empty;
        private readonly DateTime _CreateInstanceTime = DateTime.MinValue;
        private SerializeState _SerializeState = SerializeState.None;
        private ProgramCategoryCollection _Categories = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ProgramSettings()
        {
            _CreateInstanceTime = DateTime.Now;
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
            BaseFunc.SetSerializeState(_Categories, serializeState);
        }

        /// <summary>
        /// 物件執行個體的建立時間。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public DateTime CreateInstanceTime
        {
            get { return _CreateInstanceTime; }
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
        /// 設定序列化/反序列化的對應檔案。
        /// </summary>
        /// <param name="fileName">檔案名稱。</param>
        public void SetObjectFilePath(string fileName)
        {
            _ObjectFilePath = fileName;
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
