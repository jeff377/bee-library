using Bee.Business;

namespace QuickStart.Server.BusinessObjects;

/// <summary>
/// Input arguments for <see cref="EchoBusinessObject.Echo"/>.
/// </summary>
public class EchoArgs : BusinessArgs
{
    /// <summary>
    /// Gets or sets the message that the server should echo back.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
