namespace Bee.Definition.Security
{
    /// <summary>
    /// API access authentication requirement.
    /// </summary>
    public enum ApiAccessRequirement
    {
        /// <summary>
        /// No login required (anonymous access).
        /// </summary>
        Anonymous = 0,
        /// <summary>
        /// Login required (AccessToken must be validated).
        /// </summary>
        Authenticated = 1
    }
}
