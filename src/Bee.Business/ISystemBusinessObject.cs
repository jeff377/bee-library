using Bee.Api.Contracts.System;

namespace Bee.Business
{
    /// <summary>
    /// Interface for system-level business logic objects.
    /// </summary>
    public interface ISystemBusinessObject : IBusinessObject
    {
        /// <summary>
        /// Creates a new user session.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        CreateSessionResult CreateSession(CreateSessionArgs args);

        /// <summary>
        /// Gets definition data.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        GetDefineResult GetDefine(GetDefineArgs args);

        /// <summary>
        /// Saves definition data.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        SaveDefineResult SaveDefine(SaveDefineArgs args);
    }
}
