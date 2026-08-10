namespace Bee.Base.Attributes
{
    /// <summary>
    /// Marks a member as excluded from wire serialization performed by the transport layer's
    /// own formatters.
    /// </summary>
    /// <remarks>
    /// This is the framework's own vocabulary, deliberately format-neutral: it says "this member
    /// does not go over the wire", not "this member is not serialized by a particular library".
    /// Declaring it here keeps the definition layer free of any transport-format package
    /// reference — the formatters that honour it live in the API layer.
    /// <para>
    /// It does not replace <c>[XmlIgnore]</c> or <c>[JsonIgnore]</c>: those are BCL vocabulary
    /// for BCL serializers, and carry no package dependency of their own.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class WireIgnoreAttribute : Attribute
    {
    }
}
