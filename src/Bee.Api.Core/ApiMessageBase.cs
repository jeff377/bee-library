using Bee.Definition.Collections;
using System;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition;
using MessagePack;
using System.Text.Json.Serialization;

namespace Bee.Api.Core
{
    /// <summary>
    /// Base class for API message objects (requests and responses) with serialization support.
    /// </summary>
    [Serializable]
    public abstract class ApiMessageBase : IObjectSerialize
    {
        private ParameterCollection? _parameters = null;

        #region IObjectSerialize

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
            if (_parameters != null) BaseFunc.SetSerializeState(_parameters, serializeState);
        }

        #endregion

        /// <summary>
        /// Gets or sets the parameter collection.
        /// </summary>
        [Key(0)]
        public ParameterCollection? Parameters
        {
            get
            {
                // Return null when the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _parameters!)) { return null; }
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
