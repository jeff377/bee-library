namespace Bee.Definition.Security
{
    /// <summary>
    /// Classification of an API key holder. A label for operators only — it never affects whether a
    /// call is authorized, which is the job of the permission model.
    /// </summary>
    public enum ApiKeyType
    {
        /// <summary>An application built and operated by the deployment itself.</summary>
        Internal = 1,

        /// <summary>An external party integrating against the API.</summary>
        ThirdParty = 2,
    }
}
