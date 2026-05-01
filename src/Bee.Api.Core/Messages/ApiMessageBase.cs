using Bee.Definition.Collections;
using Bee.Base.Serialization;
using MessagePack;
using System.Text.Json.Serialization;

namespace Bee.Api.Core.Messages
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
            if (_parameters != null) _parameters.SetSerializeState(serializeState);
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
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _parameters!)) { return null; }
                if (_parameters == null) { _parameters = []; }
                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }
    }
}
