using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Base.Serialization;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business.System
{
    /// <summary>
    /// Business plugin maintenance half of <see cref="SystemBusinessObject"/>. Split out for file
    /// size only.
    /// </summary>
    /// <remarks>
    /// Both methods are <see cref="ApiProtectionLevel.LocalOnly"/>, on the same reasoning as
    /// <c>SaveDefine</c>: these bindings decide which code runs inside the save and delete
    /// pipelines, and choosing that is a deployment-time operation. The maintenance tool reaches
    /// them in-process through the local API provider, which is how definitions have always been
    /// maintained; a remote caller cannot reach them at all, which is a stronger guarantee than any
    /// permission check could give.
    /// </remarks>
    public partial class SystemBusinessObject
    {
        /// <summary>The customization-layer artifact these methods maintain, for audit rows.</summary>
        private const string PluginSettingsArtifact = "PluginSettings";

        /// <summary>
        /// Reads one tenant's business plugin bindings.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// Returns an empty string when the tenant declares none, which is the normal state — the
        /// caller then edits from a blank definition rather than having to distinguish "absent"
        /// from "empty".
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated)]
        public virtual GetCustomizePluginSettingsResult GetCustomizePluginSettings(GetCustomizePluginSettingsArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            // Defence in depth, as in SaveDefine: ApiAccessValidator only runs on the JSON-RPC
            // dispatch path, so a caller constructing the BO directly never passes through it.
            if (!IsLocalCall)
                throw new NotSupportedException("GetCustomizePluginSettings is restricted to local calls.");

            RequireCustomizeId(args.CustomizeId);

            var reader = Services.GetRequiredService<ICustomizeDefineReader>();
            var settings = reader.GetCustomizePluginSettings(args.CustomizeId);

            return new GetCustomizePluginSettingsResult
            {
                Xml = settings == null ? string.Empty : SerializeDefine(settings),
            };
        }

        /// <summary>
        /// Stores one tenant's business plugin bindings, replacing whatever the tenant had.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// <para>
        /// Every bound type is validated before anything is written, and one bad entry rejects the
        /// whole definition. Validating here rather than at execution time is the point: the person
        /// editing the bindings learns about a typo immediately, instead of a document failing to
        /// save weeks later.
        /// </para>
        /// <para>
        /// The whole definition is replaced, so concurrent editors overwrite one another. No
        /// optimistic locking: the operation is local-only, rare, and performed by an operator on
        /// the host.
        /// </para>
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated)]
        public virtual SaveCustomizePluginSettingsResult SaveCustomizePluginSettings(SaveCustomizePluginSettingsArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            if (!IsLocalCall)
                throw new NotSupportedException("SaveCustomizePluginSettings is restricted to local calls.");

            RequireCustomizeId(args.CustomizeId);

            var settings = DeserializePluginSettings(args.Xml);
            int pluginCount = ValidatePluginBindings(settings);

            var writer = Services.GetService<ICustomizeDefineWriter>()
                ?? throw new UserMessageException(
                    "This deployment cannot store customization plugin bindings; no customization writer is configured.");
            writer.SaveCustomizePluginSettings(args.CustomizeId, settings);

            if (DeploymentAuditEnabled())
            {
                // The customization code is the row key: it is what the change is about, and the
                // stored artifact has no row of its own to point at.
                string changes = AuditDiffGram.ForInsert(PluginSettingsArtifact,
                    [(nameof(args.CustomizeId), args.CustomizeId), (nameof(SaveCustomizePluginSettingsResult.PluginCount), pluginCount)]);
                WriteDeploymentAudit(PluginSettingsArtifact, args.CustomizeId, ChangeKind.Update, changes,
                    SystemActions.SaveCustomizePluginSettings);
            }

            return new SaveCustomizePluginSettingsResult { PluginCount = pluginCount };
        }

        /// <summary>
        /// Rejects an absent customization code. These methods address one tenant's layer, and an
        /// empty code means the base layer — which is deployment content, not tenant customization.
        /// </summary>
        private static void RequireCustomizeId(string customizeId)
        {
            if (StringUtilities.IsEmpty(customizeId))
                throw new UserMessageException("A customization code is required.");
        }

        /// <summary>
        /// Parses the supplied XML, treating a blank document as an empty definition so a tenant's
        /// bindings can be cleared without a special call.
        /// </summary>
        private static PluginSettings DeserializePluginSettings(string xml)
        {
            if (StringUtilities.IsEmpty(xml)) { return new PluginSettings(); }

            try
            {
                return XmlCodec.Deserialize<PluginSettings>(xml)
                    ?? throw new UserMessageException("The plugin settings XML is empty.");
            }
            catch (InvalidOperationException ex)
            {
                // XmlSerializer wraps every parse failure in InvalidOperationException; the inner
                // message is the one that says what is actually wrong with the document.
                throw new UserMessageException(
                    $"The plugin settings XML could not be parsed: {ex.GetBaseException().Message}", ex);
            }
        }

        /// <summary>
        /// Validates every binding and returns how many there are.
        /// </summary>
        /// <exception cref="UserMessageException">
        /// Thrown for a type that will not load, does not derive from <see cref="FormBusinessPlugin"/>,
        /// or does not override exactly the one stage its binding declares.
        /// </exception>
        private static int ValidatePluginBindings(PluginSettings settings)
        {
            int count = 0;
            foreach (var program in settings.Items ?? [])
            {
                foreach (var plugin in program.Plugins ?? [])
                {
                    ValidatePluginType(program.ProgId, plugin.Type, plugin.Stage);
                    count++;
                }
            }
            return count;
        }

        private static void ValidatePluginType(string progId, string typeName, PluginStage stage)
        {
            if (StringUtilities.IsEmpty(typeName))
                throw new UserMessageException($"Program '{progId}' declares a plugin with no type name.");

            Type? type;
            try
            {
                type = AssemblyLoader.GetType(typeName);
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                throw new UserMessageException(
                    $"Program '{progId}' declares plugin '{typeName}', which cannot be loaded.", ex);
            }

            if (type == null)
            {
                throw new UserMessageException(
                    $"Program '{progId}' declares plugin '{typeName}', which cannot be loaded.");
            }

            if (!typeof(FormBusinessPlugin).IsAssignableFrom(type))
            {
                throw new UserMessageException(
                    $"Program '{progId}' declares plugin '{typeName}', which does not derive from {nameof(FormBusinessPlugin)}.");
            }

            // The declared stage has to be exactly what the class overrides. Building the chain is
            // how that is checked, so this gate and the resolver's cannot drift apart: a definition
            // this method accepts is one the resolver can load. Accepting a mismatch would leave the
            // author believing their customization is in place while nothing happens at runtime.
            try
            {
                FormPluginChain.Create(progId, [new FormPluginBinding(type, stage)]);
            }
            catch (InvalidOperationException ex)
            {
                // The chain's message already names the stages the class actually overrides, which
                // is the fix; it only has to reach the editor as a user-facing message.
                throw new UserMessageException(ex.Message, ex);
            }
        }
    }
}
