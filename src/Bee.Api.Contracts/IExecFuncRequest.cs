namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for custom method execution request parameters.
    /// </summary>
    public interface IExecFuncRequest
    {
        /// <summary>
        /// Gets the custom method identifier.
        /// </summary>
        string FuncId { get; }
    }
}
