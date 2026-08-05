using Bee.Base;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Definition.Customization
{
    /// <summary>
    /// Decides which layer a definition value comes from once a base copy and a tenant
    /// customization copy are both in hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pure decision logic: no storage, no cache, no session, no DI. The caller is responsible for
    /// obtaining the two copies — the server from its define access, a client from two API calls —
    /// and this class only answers "which one wins". That split is what lets the server and every
    /// client run the <b>identical</b> algorithm instead of each re-implementing the overlay and
    /// drifting apart.
    /// </para>
    /// <para>
    /// Granularity is a per-type decision, encoded in the method set rather than left to callers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Language text — per key.</b> A customization resource holds only the keys it changes; every other key comes from base, so a base translation added later propagates on its own.</description></item>
    ///   <item><description><b>Language enum — whole enum.</b> An option set only means something as an ordered whole; merging entry by entry would leave both the ordering and the meaning of an omitted entry ambiguous.</description></item>
    ///   <item><description><b>ProgramSettings — per progId, then per property.</b> Each program's bindings are independent of the others, and within one program each binding is independent of its siblings: a customization that names only a business object keeps the base repository.</description></item>
    ///   <item><description><b>MenuSettings — whole file.</b> A menu is one arrangement; merging node by node would produce groupings and orderings no author chose.</description></item>
    ///   <item><description><b>FormLayout — whole file.</b> A layout is one visual arrangement; a partial merge has no intuitive answer ("this section moved — do the fields under it follow?").</description></item>
    /// </list>
    /// <para>
    /// <b>Security:</b> this class receives the customization copy, it never chooses it. Which
    /// tenant's customization is loaded stays a server-side decision driven by
    /// <c>SessionInfo.CustomizeId</c>. What is shared here is the selection algorithm, not the
    /// choice of tenant.
    /// </para>
    /// </remarks>
    public static class CustomizeOverlay
    {
        /// <summary>
        /// Resolves a single localized text, preferring the customization resource when it declares
        /// the key. Does not apply any default-language fall-back — that belongs to the caller.
        /// </summary>
        /// <param name="customize">The customization resource, or <c>null</c> when the tenant provides none.</param>
        /// <param name="base">The base resource, or <c>null</c> when the namespace has no base file.</param>
        /// <param name="subKey">The key within the resource.</param>
        /// <param name="text">The resolved text on hit; an empty string on miss.</param>
        /// <returns><c>true</c> when either layer declares the key.</returns>
        public static bool TryGetLangText(LanguageResource? customize, LanguageResource? @base, string subKey, out string text)
        {
            if (customize != null && customize.Items.Contains(subKey))
            {
                text = customize.Items[subKey].Value;
                return true;
            }
            if (@base != null && @base.Items.Contains(subKey))
            {
                text = @base.Items[subKey].Value;
                return true;
            }
            text = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves a localized enum. A customization enum of the same name replaces the base enum
        /// outright — the customization resource must list every entry the option set should have.
        /// </summary>
        /// <param name="customize">The customization resource, or <c>null</c> when the tenant provides none.</param>
        /// <param name="base">The base resource, or <c>null</c> when the namespace has no base file.</param>
        /// <param name="enumName">The enum name within the resource.</param>
        /// <returns>The customization enum, else the base enum, else <c>null</c>.</returns>
        public static LanguageEnum? GetLangEnum(LanguageResource? customize, LanguageResource? @base, string enumName)
            => customize?.GetEnum(enumName) ?? @base?.GetEnum(enumName);

        /// <summary>
        /// Resolves the program entry for a progId. Programs are independent of one another, so the
        /// overlay is per entry rather than whole-file; within an entry it is per property, so a
        /// customization declares only what it changes and every property it leaves empty keeps the
        /// base value.
        /// </summary>
        /// <param name="customize">The customization settings, or <c>null</c> when the tenant provides none.</param>
        /// <param name="base">The base settings, or <c>null</c> when none are configured.</param>
        /// <param name="progId">The program identifier.</param>
        /// <returns>
        /// The merged entry when both layers declare the progId; the single declaring entry when
        /// only one does; <c>null</c> when neither does.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Property-level inheritance is what keeps a partial customization from silently undoing a
        /// base binding. An entry now carries two independent bindings, so replacing it wholesale
        /// would mean a customization that sets only <c>BusinessObject</c> drops the base
        /// <c>Repository</c> — and an empty repository is a legal "use the framework default"
        /// rather than an error, so the loss would never be reported. To deliberately return a
        /// binding to the framework's own type, name that type explicitly instead of clearing it.
        /// </para>
        /// <para>
        /// The merged entry is always a new instance: both inputs come from a process-wide
        /// definition cache and must never be mutated (see <c>docs/development-constraints.md</c>,
        /// Definition Data Immutability After Init). When only one layer declares the progId that
        /// layer's own instance is returned as-is — there is nothing to merge, and callers only read.
        /// </para>
        /// </remarks>
        public static ProgramItem? FindProgramItem(ProgramSettings? customize, ProgramSettings? @base, string progId)
        {
            var customizeItem = customize?.Items?.GetOrDefault(progId);
            var baseItem = @base?.Items?.GetOrDefault(progId);

            if (customizeItem == null) { return baseItem; }
            if (baseItem == null) { return customizeItem; }

            // WARNING: every ProgramItem property must be merged here. One that is forgotten falls
            // back to whole-entry replacement for that property alone — exactly the silent loss this
            // method exists to prevent. `CustomizeOverlayTests` enumerates the properties by
            // reflection and fails when a new one is not covered.
            return new ProgramItem(progId, Prefer(customizeItem.DisplayName, baseItem.DisplayName))
            {
                BusinessObject = Prefer(customizeItem.BusinessObject, baseItem.BusinessObject),
                Repository = Prefer(customizeItem.Repository, baseItem.Repository),
            };
        }

        /// <summary>
        /// Returns the customization value when it says something, otherwise the base value.
        /// Whitespace counts as saying nothing, matching how the consuming factories test these
        /// bindings for emptiness.
        /// </summary>
        /// <param name="customize">The customization layer's value.</param>
        /// <param name="base">The base layer's value.</param>
        private static string Prefer(string customize, string @base)
            => StringUtilities.IsEmpty(customize) ? @base : customize;

        /// <summary>
        /// Selects the menu definition: a customization menu replaces the base menu outright.
        /// </summary>
        /// <param name="customize">The customization menu, or <c>null</c> when the tenant provides none.</param>
        /// <param name="base">The base menu, or <c>null</c> when the host defines none.</param>
        /// <returns>The customization menu, else the base menu, else <c>null</c>.</returns>
        public static MenuSettings? PickMenuSettings(MenuSettings? customize, MenuSettings? @base)
            => customize ?? @base;

        /// <summary>
        /// Selects the form layout definition: a customization layout replaces the base layout
        /// outright.
        /// </summary>
        /// <param name="customize">The customization layout, or <c>null</c> when the tenant provides none.</param>
        /// <param name="base">The base layout, or <c>null</c> when no layout definition exists.</param>
        /// <returns>The customization layout, else the base layout, else <c>null</c> — the caller
        /// then generates one from the <c>FormSchema</c>.</returns>
        public static FormLayout? PickFormLayout(FormLayout? customize, FormLayout? @base)
            => customize ?? @base;

    }
}
