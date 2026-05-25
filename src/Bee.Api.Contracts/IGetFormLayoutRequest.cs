namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get form layout request.
    /// </summary>
    public interface IGetFormLayoutRequest
    {
        /// <summary>
        /// Gets the program identifier whose layout should be retrieved.
        /// </summary>
        string ProgId { get; }

        /// <summary>
        /// Gets the layout identifier; empty string resolves to the default layout.
        /// </summary>
        string LayoutId { get; }
    }
}
