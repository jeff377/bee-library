using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Output result for saving definition data.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SaveDefineResult : BusinessResult
    {
    }
}
