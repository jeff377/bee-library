namespace Bee.Definition.Security
{
    /// <summary>
    /// Implemented by business objects that need to see which application made the current call.
    /// The transport layer assigns the verdict right after construction, before the method runs.
    /// </summary>
    /// <remarks>
    /// A setter rather than a constructor parameter on purpose: the API key verdict is
    /// transport-level information that only some methods care about (the connectivity probe
    /// reports it; the audit trail records it), and threading it through every business-object
    /// constructor would break every application subclass for the benefit of a few callers.
    /// <para>
    /// NOTE: business objects are created per call, so this stays per-call state despite being
    /// mutable. Business code only reads it.
    /// </para>
    /// </remarks>
    public interface IApiKeyContextAware
    {
        /// <summary>
        /// Gets or sets the API key verdict for the current call. Defaults to
        /// <see cref="ApiKeyValidationResult.NotChecked"/> for in-process calls, which never pass
        /// through the key gate.
        /// </summary>
        ApiKeyValidationResult ApiKeyValidation { get; set; }
    }
}
