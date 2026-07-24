using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Organization;
using Bee.Definition.Identity;
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
        /// Returns a <see cref="FormSchema"/> as a typed object, intended for JS /
        /// TypeScript frontends that prefer JSON over the XML envelope returned by
        /// <see cref="GetDefine"/>. The Plain wire format serialises the schema as
        /// a JSON tree directly; the .NET client may keep using <see cref="GetDefine"/>.
        /// </summary>
        /// <param name="args">The input arguments carrying the target <c>ProgId</c>.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetFormSchemaResult GetFormSchema(GetFormSchemaArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.ProgId))
                throw new ArgumentException("ProgId is required.", nameof(args));

            var schema = LoadAndLocalizeSchema(args.ProgId);
            return new GetFormSchemaResult { Schema = schema };
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
        /// Returns a <see cref="FormLayout"/> for the specified <c>ProgId</c> and
        /// optional <c>LayoutId</c>. The layout is generated on demand from the
        /// underlying <see cref="FormSchema"/>; for JS / TypeScript frontends the
        /// Plain wire format serialises it as a JSON tree ready for direct UI
        /// rendering.
        /// </summary>
        /// <param name="args">
        /// The input arguments. <c>ProgId</c> is required; <c>LayoutId</c> may be
        /// empty (defaults to <c>"default"</c> server-side).
        /// </param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetFormLayoutResult GetFormLayout(GetFormLayoutArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.ProgId))
                throw new ArgumentException("ProgId is required.", nameof(args));

            // Localize the schema first so the generated layout inherits localized
            // DisplayName / Caption values rather than the raw fixture text.
            var schema = LoadAndLocalizeSchema(args.ProgId);

            var layoutId = string.IsNullOrWhiteSpace(args.LayoutId) ? "default" : args.LayoutId;
            var layout = schema.GetFormLayout(layoutId);

            return new GetFormLayoutResult { Layout = layout };
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
            return new GetLanguageResult { Resource = resource };
        }

        /// <summary>
        /// Loads the <see cref="FormSchema"/> from the Define cache, deep-clones it via
        /// <see cref="FormSchema.Clone"/>, and applies localized text using the current
        /// session's <c>Culture</c>. The cloned instance is safe to mutate without
        /// affecting the shared cached schema.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The cached <see cref="FormSchema"/> is process-shared (every session reads
        /// the same in-memory instance) — see <c>docs/development-constraints.md</c>
        /// § <i>Definition Data Immutability After Init</i>. We must <b>not</b> mutate
        /// it, and we must <b>not</b> use <see cref="XmlCodec.Serialize(object)"/> as
        /// a deep-clone shortcut either: the serialization lifecycle flips
        /// <c>SerializeState</c> on the source, which races under concurrent load.
        /// </para>
        /// <para>
        /// <see cref="FormSchema.Clone"/> is a pure read of the source and produces a
        /// fully independent copy with no shared mutable state — safe under any number
        /// of concurrent callers in any combination of languages.
        /// </para>
        /// </remarks>
        /// <param name="progId">The program identifier.</param>
        private FormSchema LoadAndLocalizeSchema(string progId)
        {
            var raw = DefineAccess.GetDefine(DefineType.FormSchema, new[] { progId }) as FormSchema
                ?? throw new InvalidOperationException($"FormSchema '{progId}' not found.");

            // Both localization and number-format baking mutate the schema, so both require a clone —
            // the cached instance is process-shared and must not be touched. Skip the clone entirely
            // when there is neither a session language nor a numeric field to bake (anonymous flows
            // over a non-numeric schema shouldn't pay for a deep clone).
            string lang = GetCurrentLang();
            bool hasLang = !string.IsNullOrWhiteSpace(lang);
            bool needsBake = NumberFormatApplier.HasNumericField(raw);
            if (!hasLang && !needsBake)
                return raw;

            var clone = raw.Clone();
            if (hasLang)
                new FormSchemaLocalizer(LanguageService).Localize(clone, lang);
            if (needsBake)
                NumberFormatApplier.Bake(clone, TryGetCompanyInfo());
            return clone;
        }

        /// <summary>
        /// Resolves the current session's <see cref="CompanyInfo"/>, or <c>null</c> when there is no
        /// session or no company has been entered. Used to bake company-aware number formats; a
        /// <c>null</c> company makes the applier fall back to framework default decimals.
        /// </summary>
        private CompanyInfo? TryGetCompanyInfo()
        {
            var session = SessionInfoService.Get(AccessToken);
            if (session == null || string.IsNullOrEmpty(session.CompanyId))
                return null;
            return Services.GetRequiredService<ICompanyInfoService>().Get(session.CompanyId);
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
        /// Saves definition data (public). Sensitive definitions such as SystemSettings and DatabaseSettings are excluded.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual SaveDefineResult SaveDefine(SaveDefineArgs args)
        {
            // Non-local calls are not permitted to save SystemSettings or DatabaseSettings
            if ((args.DefineType == DefineType.SystemSettings || args.DefineType == DefineType.DatabaseSettings) && !IsLocalCall)
                throw new NotSupportedException("The specified DefineType is not supported.");

            return SaveDefineCore(args);
        }
    }
}
