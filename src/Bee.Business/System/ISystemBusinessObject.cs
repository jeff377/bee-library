namespace Bee.Business.System
{
    /// <summary>
    /// Cross-BO interface for the system-level business logic object.
    /// </summary>
    /// <remarks>
    /// <see cref="IBusinessObject"/> / <see cref="ISystemBusinessObject"/> exist as
    /// the decoupling layer for <b>BO-to-BO calls</b>: the caller resolves a BO by
    /// <c>progId</c> through <c>IBusinessObjectFactory</c>, casts to the axis
    /// interface, and invokes a method without binding to a concrete class (so
    /// host-side BO customisation does not break callers).
    ///
    /// **Pure-API methods do not belong here.** Methods that exist only to be
    /// dispatched through <c>JsonRpcExecutor.Execute</c> from a client — and
    /// have no internal BO consumers — are declared as <c>public</c> on the
    /// concrete <see cref="SystemBusinessObject"/> class with
    /// <c>[ApiAccessControl]</c>, but stay out of this interface (e.g.
    /// <c>Ping</c>, <c>GetFormSchema</c>, <c>GetFormLayout</c>, <c>GetLanguage</c>).
    /// </remarks>
    public interface ISystemBusinessObject : IBusinessObject
    {
        /// <summary>
        /// Performs the login operation.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        LoginResult Login(LoginArgs args);

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

        /// <summary>
        /// Enters the specified company for the current session.
        /// </summary>
        /// <param name="args">The input arguments carrying the target company id.</param>
        EnterCompanyResult EnterCompany(EnterCompanyArgs args);

        /// <summary>
        /// Clears the company context from the current session while keeping the session alive.
        /// </summary>
        /// <param name="args">The input arguments (currently carries no fields).</param>
        LeaveCompanyResult LeaveCompany(LeaveCompanyArgs args);

        /// <summary>
        /// Destroys the current session, clearing any company context first.
        /// </summary>
        /// <param name="args">The input arguments (currently carries no fields).</param>
        LogoutResult Logout(LogoutArgs args);
    }
}
