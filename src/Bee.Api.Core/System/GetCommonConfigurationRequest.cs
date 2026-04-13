using System;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API request for the get common configuration operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationRequest : ApiRequest, IGetCommonConfigurationRequest
    {
    }
}
