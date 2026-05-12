namespace Bee.Definition
{
    /// <summary>
    /// Empty transitional placeholder. Phase 4 stripped the service-locator role;
    /// Phase 5 cleared the remaining configuration fields (4 encryption keys +
    /// <c>LogOptions</c> + <c>LogWriter</c>) by threading them through DI ctors.
    /// Phase 6 deletes this class along with <c>BackendConfiguration</c>'s residual surface.
    /// </summary>
    public static class BackendInfo
    {
    }
}
