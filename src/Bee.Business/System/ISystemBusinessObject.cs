namespace Bee.Business.System
{
    /// <summary>
    /// Interface for system-level business logic objects.
    /// </summary>
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
        /// Gets a form schema as a typed object (JSON-friendly alternative to
        /// <see cref="GetDefine"/> for JS frontends).
        /// </summary>
        /// <param name="args">The input arguments carrying the target <c>ProgId</c>.</param>
        GetFormSchemaResult GetFormSchema(GetFormSchemaArgs args);

        /// <summary>
        /// Gets a form layout as a typed object (intended for JS frontends that
        /// need to render schema-driven UI).
        /// </summary>
        /// <param name="args">
        /// The input arguments carrying the target <c>ProgId</c> and optional
        /// <c>LayoutId</c>.
        /// </param>
        GetFormLayoutResult GetFormLayout(GetFormLayoutArgs args);

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
