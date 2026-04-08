namespace Bee.Definition
{
    /// <summary>
    /// Provides a unified access service interface for commonly used business objects in enterprise systems.
    /// Uses a caching mechanism to speed up data reads, loading from the database and deserializing on a cache miss.
    /// Suitable for organizational structures, module parameters, and other data that requires persistence and caching.
    /// </summary>
    public interface IEnterpriseObjectService
    {

    }
}
