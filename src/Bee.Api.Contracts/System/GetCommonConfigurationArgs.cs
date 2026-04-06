using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Input arguments for retrieving common parameters and environment configuration.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationArgs : BusinessArgs
    {
    }
}
