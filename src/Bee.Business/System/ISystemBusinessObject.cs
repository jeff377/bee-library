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
    /// This surface and the API surface are independent: <c>[ApiAccessControl]</c> marks what a
    /// client may call through <c>JsonRpcExecutor</c>, while this interface is what server-side
    /// code calls. Neither implies the other.
    ///
    /// A member belongs here when something inside the process resolves it through
    /// <c>IBusinessObjectFactory</c> and calls it — another business object, a background job, a
    /// scheduler. <c>Login</c> qualifies for exactly that reason: a background job signs in as a
    /// given identity to open a session, then acts as that user, filling in a form or running an
    /// operation on their behalf.
    ///
    /// The definition operations are the counter-example. <c>GetDefine</c> / <c>SaveDefine</c> are
    /// still public on <c>SystemBusinessObject</c> and still reachable by a client through
    /// <c>JsonRpcExecutor</c>, but they are deliberately not on this interface: server-side code
    /// reads and writes definitions through <c>IDefineAccess</c> directly, so routing that through
    /// another business object would buy nothing. The same reasoning covers the rest of the
    /// definition family (<c>GetFormSchema</c>, <c>GetFormLayout</c>, <c>GetLanguage</c>,
    /// <c>GetCustomize*</c>), which were never on this interface.
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
