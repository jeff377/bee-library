using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// The complete set of formatters the wire needs, registered explicitly.
    /// </summary>
    /// <remarks>
    /// WARNING: Nothing here is optional. The contractless resolver looks like it would cover most
    /// of these types, and on a desktop runtime it does — but it builds its formatters with
    /// <c>Reflection.Emit</c>, and .NET for iOS turns dynamic code off for every build
    /// (`DynamicCodeSupport=false`). There, an unregistered type is not slow, it is a
    /// <c>FormatterNotRegisteredException</c>. The same applies to the closed generic
    /// instantiations in <c>WireContracts.Generics.cs</c>: <c>List&lt;T&gt;</c>,
    /// <c>Dictionary&lt;K,V&gt;</c>, <c>T?</c> and enums are all resolved through
    /// <c>MakeGenericType</c> unless they are registered.
    /// <para>
    /// Most of this file family is generated from the wire type closure rather than written by
    /// hand; <c>WireContractDriftTests</c> is what keeps it honest, by walking the same closure at
    /// test time and failing when a type or a member is missing from the registrations.
    /// </para>
    /// </remarks>
    internal static partial class WireContracts
    {
        /// <summary>
        /// Builds the formatter list handed to <see cref="MessagePackCodec"/>.
        /// </summary>
        public static List<IMessagePackFormatter> Create()
        {
            var list = new List<IMessagePackFormatter>();
            AddFormMessages(list);
            AddSystemMessages(list);
            AddAuditLogMessages(list);
            AddEnvelopeMessages(list);
            AddDataTypes(list);
            AddDefinitionTypes(list);
            AddGenericInstantiations(list);
            return list;
        }
    }
}
