namespace Bee.Web.Blazor.Server.DependencyInjection
{
    /// <summary>
    /// Provider mode selected via <see cref="BeeBlazorOptions"/>.
    /// </summary>
    public enum BeeBlazorProviderMode
    {
        /// <summary>
        /// In-process: the host is also the API backend; connectors use
        /// <c>LocalApiProvider</c>.
        /// </summary>
        Local = 0,

        /// <summary>
        /// Over HTTP: connectors use <c>RemoteApiProvider</c> against
        /// <see cref="BeeBlazorOptions.Endpoint"/>.
        /// </summary>
        Remote = 1,
    }
}
