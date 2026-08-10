using Bee.Definition.Organization;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes <see cref="DepartmentNode"/> as a property-name keyed map, carrying only the wire members.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than reflection-driven on purpose: a generic formatter would have to
    /// recurse through the non-generic <c>Serialize(Type, ref MessagePackWriter, ...)</c> overload,
    /// which needs <c>Reflection.Emit</c> to pass the `ref struct` writer and therefore throws on
    /// the mobile heads. Naming every member at compile time keeps the whole path generic.
    /// <para>
    /// WARNING: Adding a property to <see cref="DepartmentNode"/> means adding it here too. The guard is the
    /// <see cref="WireMemberCount"/> assertion in the wire tests, which fails as soon as the two
    /// shapes drift apart.
    /// </para>
    /// </remarks>
    internal sealed class DepartmentNodeFormatter : IMessagePackFormatter<DepartmentNode?>
    {
        /// <summary>
        /// Number of members written, asserted by the wire tests.
        /// </summary>
        public const int WireMemberCount = 5;

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, DepartmentNode? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(WireMemberCount);

            writer.Write(nameof(DepartmentNode.RowId));
            MessagePackSerializer.Serialize<Guid>(ref writer, value.RowId, options);

            writer.Write(nameof(DepartmentNode.DeptId));
            MessagePackSerializer.Serialize<string>(ref writer, value.DeptId, options);

            writer.Write(nameof(DepartmentNode.DeptName));
            MessagePackSerializer.Serialize<string>(ref writer, value.DeptName, options);

            writer.Write(nameof(DepartmentNode.ManagerRowId));
            MessagePackSerializer.Serialize<Guid>(ref writer, value.ManagerRowId, options);

            writer.Write(nameof(DepartmentNode.Children));
            MessagePackSerializer.Serialize<DepartmentNodeCollection?>(ref writer, value.Children, options);
        }

        /// <summary>
        /// Deserializes the value, skipping keys this version does not know.
        /// </summary>
        public DepartmentNode? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var result = new DepartmentNode();
                var count = reader.ReadMapHeader();
                for (var i = 0; i < count; i++)
                {
                    switch (reader.ReadString())
                    {
                        case nameof(DepartmentNode.RowId):
                            result.RowId = MessagePackSerializer.Deserialize<Guid>(ref reader, options);
                            break;
                        case nameof(DepartmentNode.DeptId):
                            result.DeptId = MessagePackSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case nameof(DepartmentNode.DeptName):
                            result.DeptName = MessagePackSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case nameof(DepartmentNode.ManagerRowId):
                            result.ManagerRowId = MessagePackSerializer.Deserialize<Guid>(ref reader, options);
                            break;
                        case nameof(DepartmentNode.Children):
                            result.Children = MessagePackSerializer.Deserialize<DepartmentNodeCollection?>(ref reader, options);
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                return result;
            }
            finally
            {
                reader.Depth--;
            }
        }
    }
}
