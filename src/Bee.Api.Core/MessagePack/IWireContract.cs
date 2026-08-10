namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Exposes the wire member list of a registered contract so tests can compare it against the
    /// type's actual shape.
    /// </summary>
    /// <remarks>
    /// Nothing in production consumes this. It exists because the compiler cannot tie a wire type
    /// to its registration: without a drift check, adding a property to a wire type silently keeps
    /// it off the wire. <c>WireContractDriftTests</c> is that check.
    /// </remarks>
    internal interface IWireContract
    {
        /// <summary>
        /// Gets the type this contract covers.
        /// </summary>
        Type WireType { get; }

        /// <summary>
        /// Gets the member names written to the wire, in write order.
        /// </summary>
        IReadOnlyList<string> WireMemberNames { get; }
    }
}
