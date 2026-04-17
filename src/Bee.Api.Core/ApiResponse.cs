using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// Base class for API response objects with serialization support.
    /// </summary>
    [Serializable]
    public abstract class ApiResponse : ApiMessageBase
    {
    }
}
