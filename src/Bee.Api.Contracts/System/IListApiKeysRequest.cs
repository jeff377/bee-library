namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the list API keys request. Carries no criteria: the whole set is
    /// small and every key matters when deciding what to rotate.
    /// </summary>
    public interface IListApiKeysRequest
    {
    }
}
