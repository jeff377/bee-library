namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the get language resource request.
    /// </summary>
    public interface IGetLanguageRequest
    {
        /// <summary>
        /// Gets the BCP-47 language code (e.g. <c>"zh-TW"</c>, <c>"en-US"</c>)
        /// of the resource to retrieve.
        /// </summary>
        string Lang { get; }

        /// <summary>
        /// Gets the resource namespace (matches the file name stem; e.g.
        /// <c>"Common"</c>, <c>"Customer"</c>).
        /// </summary>
        string Namespace { get; }
    }
}
