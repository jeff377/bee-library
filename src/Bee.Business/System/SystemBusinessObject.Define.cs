using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Organization;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// Definition-access half of <see cref="SystemBusinessObject"/> (get / save define, form schema,
    /// layout, language and department tree). Split out for file size only; behaviour is unchanged.
    /// </summary>
    public partial class SystemBusinessObject
    {
        /// <summary>
        /// Core method for retrieving definition data.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        private GetDefineResult GetDefineCore(GetDefineArgs args)
        {
            var result = new GetDefineResult();
            object value = DefineAccess.GetDefine(args.DefineType, args.Keys);

            if (value != null)
            {
                // If the definition implements ISerializableClone, create a copy first
                // to avoid polluting the cache during serialization
                if (value is ISerializableClone cloneable)
                {
                    value = cloneable.CreateSerializableCopy();
                }
                // Serialize the object to XML
                result.Xml = XmlCodec.Serialize(value);
            }

            return result;
        }

        /// <summary>
        /// Gets definition data (public). Sensitive definitions such as SystemSettings and DatabaseSettings are excluded.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetDefineResult GetDefine(GetDefineArgs args)
        {
            // Non-local calls are not permitted to access SystemSettings or DatabaseSettings
            if ((args.DefineType == DefineType.SystemSettings || args.DefineType == DefineType.DatabaseSettings) && !IsLocalCall)
                throw new NotSupportedException("The specified DefineType is not supported.");
            return GetDefineCore(args);
        }

        /// <summary>
        /// Returns the raw <see cref="FormSchema"/> definition as XML.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The per-type entry point for ordinary clients, as against <see cref="GetDefine"/>, which
        /// serves every definition type and is gated for tooling. Both carry XML and both serve the
        /// definition <b>as stored</b>: no localization, no number-format baking, no customization
        /// overlay. Callers apply those themselves — <c>FormSchemaLocalizer</c>,
        /// <c>NumberFormatApplier</c> and <c>CustomizeOverlay</c> all live in <c>Bee.Definition</c>
        /// so the server and every client run the identical code.
        /// </para>
        /// <para>
        /// XML rather than a JSON tree because definition types declare XML as their serialisation
        /// contract: their nested collections are get-only, which XmlSerializer handles by
        /// populating the existing instance, while JSON and MessagePack bind by writability and
        /// would silently drop those collections on the way back.
        /// </para>
        /// </remarks>
        /// <param name="args">The input arguments carrying the target <c>ProgId</c>.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetFormSchemaResult GetFormSchema(GetFormSchemaArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.ProgId))
                throw new ArgumentException("ProgId is required.", nameof(args));

            var schema = DefineAccess.GetDefine(DefineType.FormSchema, new[] { args.ProgId }) as FormSchema
                ?? throw new InvalidOperationException($"FormSchema '{args.ProgId}' not found.");
            return new GetFormSchemaResult { Xml = SerializeDefine(schema) };
        }

        /// <summary>
        /// Returns the current company's department tree (per-company organisation hierarchy),
        /// scoped to the session's company. JSON-friendly for JS frontends; the tree is
        /// <c>null</c> when no company has been entered.
        /// </summary>
        /// <param name="args">The input arguments (carries no fields).</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetDepartmentTreeResult GetDepartmentTree(GetDepartmentTreeArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            var sessionInfo = SessionInfoService.Get(AccessToken)
                ?? throw new UnauthorizedAccessException("Session not found or has expired.");

            DepartmentTree? tree = null;
            if (!string.IsNullOrEmpty(sessionInfo.CompanyId))
            {
                tree = Services.GetRequiredService<IDepartmentTreeService>().Get(sessionInfo.CompanyId);
            }
            return new GetDepartmentTreeResult { Tree = tree };
        }

        /// <summary>
        /// Returns the raw base-layer <see cref="FormLayout"/> definition as XML, or an empty
        /// string when no layout is stored for that identifier.
        /// </summary>
        /// <remarks>
        /// Serves the definition as stored — the layout is <b>not</b> generated from the
        /// <see cref="FormSchema"/> here, and the customization layer is a separate call. A caller
        /// assembles the runtime layout itself: fetch this and the customization layout, pick
        /// between them with <c>CustomizeOverlay</c>, generate from the schema when neither exists,
        /// and take the captions from the localized schema. An empty result is a normal answer
        /// meaning "no layout definition — generate one".
        /// </remarks>
        /// <param name="args">
        /// The input arguments. <c>ProgId</c> is required; an empty <c>LayoutId</c> resolves to
        /// <c>ProgId</c>, matching the <c>{ProgId}.FormLayout.xml</c> file convention.
        /// </param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetFormLayoutResult GetFormLayout(GetFormLayoutArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.ProgId))
                throw new ArgumentException("ProgId is required.", nameof(args));

            // An empty LayoutId means "this form's own layout". It resolves to the ProgId rather
            // than a literal "default" because layout definition files are named after the progId
            // ({ProgId}.FormLayout.xml, LayoutId == ProgId); "default" would never match a file.
            var layoutId = string.IsNullOrWhiteSpace(args.LayoutId) ? args.ProgId : args.LayoutId;

            // Base layer only. The caller fetches the customization layer through
            // GetCustomizeFormLayout and picks between them with CustomizeOverlay, and generates
            // one from the FormSchema when neither layer has a definition.
            var layout = DefineAccess.FindFormLayout(string.Empty, layoutId);
            return new GetFormLayoutResult { Xml = layout is null ? string.Empty : SerializeDefine(layout) };
        }

        /// <summary>
        /// Returns a <see cref="LanguageResource"/> as a typed object — JS / TypeScript
        /// frontends consume the result through the Plain JSON wire format.
        /// </summary>
        /// <remarks>
        /// <para>
        /// **JS-only API.** The <see cref="LanguageResource"/> family uses
        /// <c>KeyCollectionBase</c> internals that do not round-trip through
        /// MessagePack (the Encoded / Encrypted wire formats); the Plain JSON wire
        /// path used by JS / TypeScript clients works correctly. Sibling methods
        /// <see cref="GetFormSchema"/> and <see cref="GetFormLayout"/> follow the
        /// same convention. .NET clients should use <see cref="GetDefine"/> with
        /// <c>DefineType.Language</c> for the XML-based access path.
        /// </para>
        /// <para>
        /// The resource is read from the Define cache via
        /// <c>IDefineAccess.GetLanguage</c> and returned as-is. Per
        /// <c>docs/development-constraints.md § Definition Data Immutability After Init</c>,
        /// the cached instance must not be mutated; callers that need per-session
        /// adjustments should clone the result.
        /// </para>
        /// </remarks>
        /// <param name="args">The input arguments carrying <c>Lang</c> and <c>Namespace</c>.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetLanguageResult GetLanguage(GetLanguageArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.Lang))
                throw new ArgumentException("Lang is required.", nameof(args));
            if (string.IsNullOrWhiteSpace(args.Namespace))
                throw new ArgumentException("Namespace is required.", nameof(args));

            // GetLanguage returns null when the resource file does not exist;
            // that is a normal scenario (missing translation), not an error.
            var resource = DefineAccess.GetLanguage(args.Lang, args.Namespace);
            return new GetLanguageResult { Xml = resource is null ? string.Empty : SerializeDefine(resource) };
        }

        /// <summary>
        /// Serializes a definition for the wire, copying first when the type asks for it so the
        /// serialization lifecycle never touches the process-shared cache instance.
        /// </summary>
        /// <param name="define">The definition object taken from the Define cache.</param>
        private static string SerializeDefine(object define)
        {
            if (define is ISerializableClone cloneable)
                define = cloneable.CreateSerializableCopy();
            return XmlCodec.Serialize(define);
        }

        /// <summary>
        /// Core method for saving definition data.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        private SaveDefineResult SaveDefineCore(SaveDefineArgs args)
        {
            // Deserialize XML to the target object
            var type = args.DefineType.ToClrType();
            object? defineObject = XmlCodec.Deserialize(args.Xml, type);
            if (defineObject == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {type.Name} object.");

            // Save the definition data
            DefineAccess.SaveDefine(args.DefineType, defineObject, args.Keys);
            var result = new SaveDefineResult();
            return result;
        }

        /// <summary>
        /// Saves definition data. Restricted to local calls.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Writing a definition is a deployment-time operation, not an application one, so this
        /// method is <see cref="ApiProtectionLevel.LocalOnly"/> — stricter than any token- or
        /// permission-based check a remote caller could satisfy. Tooling that maintains
        /// definitions (the define editor, deployment scripts) runs against a local connection;
        /// remote clients read definitions through <see cref="GetDefine"/> and never write them.
        /// </para>
        /// <para>
        /// The previous guard only rejected <c>SystemSettings</c> and <c>DatabaseSettings</c> from
        /// remote callers, which left every other definition type writable by any authenticated
        /// account — including <c>PermissionModels</c> (the authorisation model itself),
        /// <c>DbCategorySettings</c> (which database each table resolves to) and <c>FormSchema</c>
        /// (whose expressions are evaluated server-side). Gating the whole method removes that
        /// class of escalation rather than enumerating the sensitive types.
        /// </para>
        /// </remarks>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated)]
        public virtual SaveDefineResult SaveDefine(SaveDefineArgs args)
        {
            // Defence in depth, deliberately kept alongside the LocalOnly attribute rather than
            // relying on it alone: ApiAccessValidator only runs on the JSON-RPC dispatch path, so a
            // caller that constructs the BO directly — in-process hosting, a custom dispatcher, a
            // subclass — never passes through it. The attribute stops remote API traffic; this stops
            // everything else.
            if (!IsLocalCall)
                throw new NotSupportedException("SaveDefine is restricted to local calls.");

            return SaveDefineCore(args);
        }
    }
}
