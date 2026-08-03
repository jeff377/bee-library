using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for listing the issued API keys. Empty by design — see
    /// <see cref="IListApiKeysRequest"/>.
    /// </summary>
    public class ListApiKeysArgs : BusinessArgs, IListApiKeysRequest
    {
    }
}
