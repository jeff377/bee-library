using Bee.Api.Contracts;

namespace Bee.Business
{
    /// <summary>
    /// Base interface for business logic objects.
    /// </summary>
    public interface IBusinessObject
    {
        /// <summary>
        /// Executes a custom method; requires authentication.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        ExecFuncResult ExecFunc(ExecFuncArgs args);

        /// <summary>
        /// Executes a custom method; allows anonymous access.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        ExecFuncResult ExecFuncAnonymous(ExecFuncArgs args);
    }
}
