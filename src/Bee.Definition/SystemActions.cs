namespace Bee.Definition
{
    /// <summary>
    /// Action name constants for the SystemObject.
    /// </summary>
    public class SystemActions
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
