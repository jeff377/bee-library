namespace Bee.Web.Blazor.Server.DependencyInjection
{
    /// <summary>
    /// Provider mode selected via <see cref="BeeBlazorOptions"/>.
    /// </summary>
    public enum BeeBlazorProviderMode
    {
        /// <summary>
        /// In-process: the host is also the API backend; connectors use
        /// <see cref="Bee.Api.Client.Providers.LocalApiProvider"/>.
        /// </summary>
        Local = 0,

        /// <summary>
        /// Over HTTP: connectors use <see cref="Bee.Api.Client.Providers.RemoteApiProvider"/> against
        /// <see cref="BeeBlazorOptions.Endpoint"/>.
        /// </summary>
        Remote = 1,
    }
}
