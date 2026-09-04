using System.Diagnostics.CodeAnalysis;
using Bee.Base.Exceptions;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// The single declaration of which exception carries which JSON-RPC error code, consumed by
    /// both ends of the wire: the executor maps an exception to a code, and the client rebuilds
    /// an exception from that code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two directions used to be two hand-written chains of <c>if</c> in two assemblies, kept
    /// in step by nothing but the habit of remembering. That is a structural problem rather than a
    /// discipline one, and it failed exactly as predicted: the executor learned to produce
    /// <see cref="JsonRpcErrorCode.ReplayRejected"/> while the client never learned to rebuild it,
    /// so the <c>catch</c> that <see cref="ReplayRejectedException"/> documents was unreachable for as long
    /// as the code compiled and the tests passed.
    /// </para>
    /// <para>
    /// IMPORTANT: the table is ordered, and it has to be. Matching is by assignability rather than
    /// exact type — that is what lets <see cref="ArgumentNullException"/> arrive under
    /// <see cref="ArgumentException"/> — so a base type placed ahead of its own subclass would
    /// swallow it. The invariant is therefore: <b>a derived type must be declared before any of its
    /// base types</b>. Nothing about the ordering is left to a reader's care;
    /// <c>ErrorContractDriftTests</c> asserts it.
    /// </para>
    /// <para>
    /// Two things deliberately stay out of this table. The fallback to
    /// <see cref="JsonRpcErrorCode.InternalError"/> is a policy about what an unrecognized failure
    /// may reveal (and reads <see cref="Bee.Base.SysInfo.IsDebugMode"/> to decide), not a correspondence between a
    /// type and a code; and the client's generic branch is a message format, not a mapping. Both
    /// live where they are applied.
    /// </para>
    /// </remarks>
    public static class JsonRpcErrorContract
    {
        /// <summary>
        /// One row of the contract: an exception type, the code it travels as, and — when this
        /// type is the one the client rebuilds for that code — how to rebuild it.
        /// </summary>
        /// <param name="ExceptionType">The server-side exception type.</param>
        /// <param name="Code">The code this type is sent as.</param>
        /// <param name="Rebuild">
        /// The factory the client uses for <paramref name="Code"/>, or <c>null</c> when this row
        /// only feeds the outbound direction. At most one row per code carries a factory.
        /// </param>
        private sealed record Mapping(
            Type ExceptionType,
            JsonRpcErrorCode Code,
            Func<string, Exception>? Rebuild);

        /// <summary>
        /// The contract itself. Derived types first — see the ordering invariant in the type
        /// remarks.
        /// </summary>
        /// <remarks>
        /// The rows that collapse into <see cref="JsonRpcErrorCode.UserMessage"/> without rebuilding
        /// — <see cref="UnauthorizedAccessException"/>, <see cref="ArgumentException"/>,
        /// <see cref="InvalidOperationException"/>, <see cref="NotSupportedException"/>,
        /// <see cref="FormatException"/> and <see cref="JsonRpcException"/> — are a transition path,
        /// not the destination: <see cref="UserMessageException"/> is the type new business code
        /// should throw, and these are meant to be retired once the code that throws them has
        /// migrated. Removing a row narrows what reaches the caller as a readable message, so each
        /// one goes when its callers do, not before.
        /// <para>
        /// NOTE: named rather than counted. This said "the six BCL rows", which had the count right
        /// and the label wrong — <see cref="JsonRpcException"/> is the framework's own type, so no
        /// wording with a number in it was true. A wrong name is visible to the reader; a wrong
        /// count is not (<c>code-style.md</c>, "不寫程式碼構件的清點數字").
        /// </para>
        /// </remarks>
        private static readonly Mapping[] s_mappings =
        [
            new(typeof(CompanyNotEnteredException), JsonRpcErrorCode.CompanyNotEntered,
                message => new CompanyNotEnteredException(message)),
            new(typeof(CompanyAccessDeniedException), JsonRpcErrorCode.CompanyAccessDenied,
                message => new CompanyAccessDeniedException(message)),
            new(typeof(ForbiddenException), JsonRpcErrorCode.PermissionDenied,
                message => new ForbiddenException(message)),
            new(typeof(ReplayRejectedException), JsonRpcErrorCode.ReplayRejected,
                message => new ReplayRejectedException(message)),

            // The canonical row for UserMessage, and the only one of the group that rebuilds:
            // the code is many-to-one on the way out, so the way back can only land here.
            new(typeof(UserMessageException), JsonRpcErrorCode.UserMessage,
                message => new UserMessageException(message)),

            new(typeof(UnauthorizedAccessException), JsonRpcErrorCode.UserMessage, null),
            new(typeof(ArgumentException), JsonRpcErrorCode.UserMessage, null),
            new(typeof(InvalidOperationException), JsonRpcErrorCode.UserMessage, null),
            new(typeof(NotSupportedException), JsonRpcErrorCode.UserMessage, null),
            new(typeof(FormatException), JsonRpcErrorCode.UserMessage, null),
            new(typeof(JsonRpcException), JsonRpcErrorCode.UserMessage, null),
        ];

        /// <summary>
        /// The contract in declaration order, for tests that assert its shape.
        /// </summary>
        internal static IReadOnlyList<(Type ExceptionType, JsonRpcErrorCode Code, bool CanRebuild)> Rows { get; }
            = s_mappings
                .Select(mapping => (mapping.ExceptionType, mapping.Code, mapping.Rebuild != null))
                .ToArray();

        /// <summary>
        /// Finds the code an exception travels as.
        /// </summary>
        /// <param name="exception">The exception, already unwrapped.</param>
        /// <param name="code">The code to send, when one is declared for this type.</param>
        /// <returns>
        /// <c>true</c> when the contract covers this exception; <c>false</c> when the caller should
        /// apply its own fallback.
        /// </returns>
        /// <remarks>
        /// Internal because the executor is the only mapper in this direction. Widening it later is
        /// additive, so it stays closed until something outside this assembly needs it.
        /// </remarks>
        internal static bool TryGetCode(Exception exception, out JsonRpcErrorCode code)
        {
            ArgumentNullException.ThrowIfNull(exception);

            foreach (var mapping in s_mappings)
            {
                if (mapping.ExceptionType.IsInstanceOfType(exception))
                {
                    code = mapping.Code;
                    return true;
                }
            }

            code = default;
            return false;
        }

        /// <summary>
        /// Rebuilds the exception a code stands for, so a caller can branch on the type rather than
        /// on an integer.
        /// </summary>
        /// <param name="code">The <c>error.code</c> read off the response.</param>
        /// <param name="message">The message to carry over verbatim, without any prefix.</param>
        /// <param name="exception">The rebuilt exception, when the code declares one.</param>
        /// <returns>
        /// <c>true</c> when the code declares an exception type; <c>false</c> for codes that are
        /// deliberately left to the caller's generic branch, and for codes this build does not know.
        /// </returns>
        /// <remarks>
        /// Takes an <see cref="int"/> rather than <see cref="JsonRpcErrorCode"/> on purpose: the
        /// value arrives off the wire and a newer server may send a code this build has never heard
        /// of. Casting that to the enum first would manufacture a value with no member behind it.
        /// </remarks>
        public static bool TryRebuild(int code, string message, [NotNullWhen(true)] out Exception? exception)
        {
            foreach (var mapping in s_mappings)
            {
                if (mapping.Rebuild != null && (int)mapping.Code == code)
                {
                    exception = mapping.Rebuild(message);
                    return true;
                }
            }

            exception = null;
            return false;
        }
    }
}
