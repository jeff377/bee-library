using System;
using Bee.Base;
using MessagePack;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 商業邏輯物件方法傳入引數基底類別。
    /// </summary>
    [Serializable]
    public abstract class TBusinessArgs : IObjectSerialize
    {
        private TParameterCollection _parameters = null;

        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore, IgnoreMember]
        public ESerializeState SerializeState { get; private set; } = ESerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(ESerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_parameters, serializeState);
        }

        #endregion

        /// <summary>
        /// 傳入參數集合。
        /// </summary>
        [Key(0)]
        public TParameterCollection Parameters
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _parameters)) { return null; }
                if (_parameters == null) { _parameters = new TParameterCollection(); }
                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }
    }
}
