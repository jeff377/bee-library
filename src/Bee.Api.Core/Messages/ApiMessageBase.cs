using Bee.Definition.Collections;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Api.Core.Messages
{
    /// <summary>
    /// Base class for API message objects (requests and responses) with serialization support.
    /// </summary>
    public abstract class ApiMessageBase : IObjectSerialize
    {
        private ParameterCollection? _parameters = null;

        #region IObjectSerialize

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [JsonIgnore]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            _parameters?.SetSerializeState(serializeState);
        }

        #endregion

        /// <summary>
        /// Gets or sets the parameter collection.
        /// </summary>
        /// <remarks>
        /// <b>Values here are never time-zone converted.</b> The bag is untyped, so a
        /// <see cref="DateTime"/> inside it carries no marker saying whether it is an instant, a
        /// calendar day, or a system timestamp that is already UTC — converting every one of them
        /// would be a guess, and would corrupt values a caller deliberately put in as UTC.
        /// <para>
        /// A custom (AnyCode) method that needs to pass an instant should agree its own basis with
        /// the caller — UTC is the framework-consistent choice — or carry the value in a
        /// <c>DataTable</c>, where the <see cref="Bee.Base.Data.FieldDbType"/> marker makes the intent explicit and the
        /// connector converts it. See <c>docs/adr/adr-032-datetime-timezone.md</c> (D4).
        /// </para>
        /// </remarks>
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
