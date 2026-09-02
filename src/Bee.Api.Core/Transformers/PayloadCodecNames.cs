namespace Bee.Api.Core.Transformers
{
    /// <summary>
    /// The identifiers a payload uses to name its body codec on the wire.
    /// </summary>
    /// <remarks>
    /// These strings travel in the payload envelope and are matched ordinally, so they are part of
    /// the wire format: renaming one breaks every client already sending it.
    /// </remarks>
    public static class PayloadCodecNames
    {
        /// <summary>
        /// The MessagePack body codec — the framework default, and what a payload that names no
        /// codec is taken to mean.
        /// </summary>
        public const string MessagePack = "messagepack";

        /// <summary>
        /// The JSON body codec, for clients that cannot speak the MessagePack wire.
        /// </summary>
        public const string Json = "json";
    }
}
