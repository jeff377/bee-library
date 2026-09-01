using System.Buffers.Binary;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// The anti-replay header carried inside the encrypted envelope, immediately ahead of the
    /// payload body.
    /// </summary>
    /// <remarks>
    /// The frame is prepended after <c>Encode</c> (serialize + compress) and before encryption, so
    /// the payload HMAC covers it and it cannot be rewritten in transit. Placing these values on
    /// <see cref="ApiPayload"/> instead would put them in the plaintext envelope, where an attacker
    /// could simply overwrite them.
    /// <para>
    /// The frame carries no length prefix and the body follows it with no separator, so a reader
    /// must know the frame's width before it can reach the body. That is what <see cref="Version"/>
    /// is for: a future layout can be introduced without a second breaking change. In version 1 the
    /// byte contributes nothing to security — an attacker can forge it and still has to pass the
    /// HMAC — but it lets a server reject an old client with a clear message instead of reading the
    /// body as a nonsensical timestamp.
    /// </para>
    /// </remarks>
    public sealed class ApiPayloadFrame
    {
        /// <summary>The frame layout this build writes.</summary>
        public const byte CurrentVersion = 1;

        /// <summary>The width in bytes of a version 1 frame.</summary>
        public const int Version1Size = 17;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiPayloadFrame"/> class at
        /// <see cref="CurrentVersion"/>.
        /// </summary>
        /// <param name="timestampMs">Unix time in milliseconds at which the request was formed.</param>
        /// <param name="sequence">
        /// The per-session monotonic sequence number. Stage 1 of the anti-replay work leaves this at
        /// zero and the server does not read it; the field is present so that enabling the sequence
        /// window later needs no second frame layout.
        /// </param>
        public ApiPayloadFrame(long timestampMs, long sequence)
            : this(CurrentVersion, timestampMs, sequence)
        {
        }

        private ApiPayloadFrame(byte version, long timestampMs, long sequence)
        {
            Version = version;
            TimestampMs = timestampMs;
            Sequence = sequence;
        }

        /// <summary>Gets the frame layout version.</summary>
        public byte Version { get; }

        /// <summary>Gets the Unix time in milliseconds at which the request was formed.</summary>
        public long TimestampMs { get; }

        /// <summary>Gets the per-session monotonic sequence number.</summary>
        public long Sequence { get; }

        /// <summary>
        /// Returns a new array holding this frame followed by <paramref name="body"/>.
        /// </summary>
        /// <param name="body">The encoded payload body.</param>
        /// <returns>The framed bytes, ready to be encrypted.</returns>
        public byte[] Prepend(byte[] body)
        {
            ArgumentNullException.ThrowIfNull(body);

            var framed = new byte[Version1Size + body.Length];
            framed[0] = Version;
            BinaryPrimitives.WriteInt64BigEndian(framed.AsSpan(1, 8), TimestampMs);
            BinaryPrimitives.WriteInt64BigEndian(framed.AsSpan(9, 8), Sequence);
            body.CopyTo(framed, Version1Size);
            return framed;
        }

        /// <summary>
        /// Splits a decrypted buffer into its frame and body.
        /// </summary>
        /// <param name="framed">The decrypted bytes, expected to begin with a frame.</param>
        /// <param name="body">On return, the payload body with the frame removed.</param>
        /// <returns>The frame that was read.</returns>
        /// <exception cref="ReplayRejectedException">
        /// Thrown when the buffer is too short to hold a frame, or when its version byte is not one
        /// this build understands. Both mean the caller did not send a frame the server can read —
        /// most often a client predating the feature — and the call is refused rather than run
        /// without replay protection.
        /// </exception>
        public static ApiPayloadFrame Extract(byte[] framed, out byte[] body)
        {
            ArgumentNullException.ThrowIfNull(framed);

            if (framed.Length < Version1Size)
            {
                throw new ReplayRejectedException(
                    "The payload is too short to contain a wire frame. The client is likely older than the server's replay-protection requirement.");
            }

            byte version = framed[0];
            if (version != CurrentVersion)
            {
                throw new ReplayRejectedException(
                    $"Unsupported wire frame version {version}. Update the client to match the server.");
            }

            long timestampMs = BinaryPrimitives.ReadInt64BigEndian(framed.AsSpan(1, 8));
            long sequence = BinaryPrimitives.ReadInt64BigEndian(framed.AsSpan(9, 8));

            body = new byte[framed.Length - Version1Size];
            Array.Copy(framed, Version1Size, body, 0, body.Length);

            return new ApiPayloadFrame(version, timestampMs, sequence);
        }
    }
}
