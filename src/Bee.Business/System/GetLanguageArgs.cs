using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for retrieving a language resource as a typed object.
    /// </summary>
    public class GetLanguageArgs : BusinessArgs, IGetLanguageRequest
    {
        /// <summary>
        /// Gets or sets the BCP-47 language code (e.g. <c>"zh-TW"</c>, <c>"en-US"</c>).
        /// </summary>
        public string Lang { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;
    }
}
