using System;
using Bee.Base;
using MessagePack;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 業務邏輯物件方法傳出結果基底類別。
    /// </summary>
    [Serializable]
    public abstract class BusinessResult : IObjectSerialize
    {
        private ParameterCollection _parameters = null;

        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore, IgnoreMember]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_parameters, serializeState);
        }

        #endregion

        /// <summary>
        /// 傳出參數集合。
        /// </summary>
        [Key(0)]
        public ParameterCollection Parameters
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(SerializeState, _parameters)) { return null; }
                if (_parameters == null) { _parameters = new ParameterCollection(); }
                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }
    }
}
