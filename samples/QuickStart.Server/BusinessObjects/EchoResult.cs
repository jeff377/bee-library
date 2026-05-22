using Bee.Business;

namespace QuickStart.Server.BusinessObjects;

/// <summary>
/// Output result for <see cref="EchoBusinessObject.Echo"/>.
/// </summary>
public class EchoResult : BusinessResult
{
    /// <summary>
    /// Gets or sets the echoed message decorated with a server-side prefix.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server UTC time when the response was produced.
    /// </summary>
    public DateTime ServerTime { get; set; }
}
