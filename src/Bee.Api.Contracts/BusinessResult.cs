using Bee.Define.Collections;
using System;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Define;
using MessagePack;
using Newtonsoft.Json;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Base class for output results of business logic object methods.
    /// </summary>
    [Serializable]
    public abstract class BusinessResult : IObjectSerialize
    {
        private ParameterCollection _parameters = null;

        #region IObjectSerialize 介面

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [JsonIgnore, IgnoreMember]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_parameters, serializeState);
        }

        #endregion

        /// <summary>
        /// Gets or sets the output parameter collection.
        /// </summary>
        [Key(0)]
        public ParameterCollection Parameters
        {
            get
            {
                // Return null when the collection is empty during serialization
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
