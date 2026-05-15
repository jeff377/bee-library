namespace Bee.Definition
{
    /// <summary>
    /// Action name constants for the SystemObject.
    /// </summary>
    public static class SystemActions
    {
        /// <summary>
        /// Ping method — tests whether the API service is available. Data encoding is not enabled for this method.
        /// </summary>
        public const string Ping = "Ping";
        /// <summary>
        /// Gets common configuration parameters and environment settings. Data encoding is not enabled for this method.
        /// </summary>
        public const string GetCommonConfiguration = "GetCommonConfiguration";
        /// <summary>
        /// Performs the login operation. Encryption is not enabled for this method.
        /// </summary>
        public const string Login = "Login";
        /// <summary>
        /// Destroys the current session, clearing any company context first.
        /// </summary>
        public const string Logout = "Logout";
        /// <summary>
        /// Enters the specified company for the current session. Also used to switch
        /// between companies (the previous CompanyId is overwritten).
        /// </summary>
        public const string EnterCompany = "EnterCompany";
        /// <summary>
        /// Clears the company context from the current session while keeping the session alive.
        /// </summary>
        public const string LeaveCompany = "LeaveCompany";
        /// <summary>
        /// Creates a session. Data encoding is not enabled for this method.
        /// </summary>
        public const string CreateSession = "CreateSession";
        /// <summary>
        /// Gets definition data.
        /// </summary>
        public const string GetDefine = "GetDefine";
        /// <summary>
        /// Gets definition data (local only).
        /// </summary>
        public const string GetLocalDefine = "GetLocalDefine";
        /// <summary>
        /// Saves definition data.
        /// </summary>
        public const string SaveDefine = "SaveDefine";
        /// <summary>
        /// Saves definition data (local only).
        /// </summary>
        public const string SaveLocalDefine = "SaveLocalDefine";
        /// <summary>
        /// Executes a custom function.
        /// </summary>
        public const string ExecFunc = "ExecFunc";
        /// <summary>
        /// Executes a custom function with anonymous access.
        /// </summary>
        public const string ExecFuncAnonymous = "ExecFuncAnonymous";
        /// <summary>
        /// Executes a custom function — local calls only.
        /// </summary>
        public const string ExecFuncLocal = "ExecFuncLocal";
    }
}
