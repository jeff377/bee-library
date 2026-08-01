using Bee.Base.Serialization;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests.Customization
{
    /// <summary>
    /// Per-class fixture for the tenant-customization end-to-end tests: shared databases (so
    /// <c>st_company</c> / <c>st_user_company</c> exist and carry the seed user) plus a populated
    /// customization folder for one tenant code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The files land under <c>{TestProcessBootstrap.SharedCustomizePath}/{CustomizeId}/</c> — the
    /// same customization root the bootstrap container reads, which is what makes them visible to
    /// near-end API calls. <see cref="CustomizeId"/> is unique to this test class, so no other class
    /// sees the overrides; the whole root is a per-process temp directory removed on process exit,
    /// which is why nothing is deleted here.
    /// </para>
    /// <para>
    /// The language override deliberately covers only two sub-keys. Everything else must still come
    /// from the base resource — that is the per-key overlay under test, and copying the whole file
    /// would hide a regression in it.
    /// </para>
    /// </remarks>
    public sealed class TenantCustomizationFixture : BeeTestFixture
    {
        /// <summary>The customization code of the tenant that ships overrides.</summary>
        public const string CustomizeId = "E2ETENANT";

        /// <summary>A customization code with no folder under the customization root.</summary>
        public const string UncustomizedId = "E2EOTHER";

        /// <summary>The program the overrides target.</summary>
        public const string ProgId = "Customer";

        /// <summary>The language the overrides are written in.</summary>
        public const string Lang = "zh-TW";

        /// <summary>The field whose caption the tenant renames.</summary>
        public const string OverriddenField = "sys_name";

        /// <summary>The tenant's caption for <see cref="OverriddenField"/>.</summary>
        public const string OverriddenCaption = "客戶抬頭（客製）";

        /// <summary>The tenant's display name for the schema.</summary>
        public const string OverriddenSchemaDisplayName = "客戶主檔（客製）";

        /// <summary>A field the tenant leaves alone, to prove the per-key fall-back to base.</summary>
        public const string InheritedField = "city";

        /// <summary>The base caption of <see cref="InheritedField"/> in <see cref="Lang"/>.</summary>
        public const string InheritedCaption = "城市";

        /// <summary>The base caption of <see cref="OverriddenField"/> in <see cref="Lang"/>.</summary>
        public const string BaseCaption = "公司名稱";

        /// <summary>The base display name of the schema in <see cref="Lang"/>.</summary>
        public const string BaseSchemaDisplayName = "客戶";

        /// <summary>The business object the tenant binds <see cref="ProgId"/> to.</summary>
        public const string OverriddenBusinessObject =
            "Bee.Api.Client.UnitTests.Customization.TenantCustomerBusinessObject, Bee.Api.Client.UnitTests";

        /// <summary>
        /// How many fields the tenant's layout keeps. Deliberately fewer than the base layout file
        /// has, so "the tenant's file won" is visible from the field count alone.
        /// </summary>
        public const int OverriddenLayoutFieldCount = 2;

        /// <summary>
        /// Creates the fixture and writes the tenant's customization files.
        /// </summary>
        public TenantCustomizationFixture() : base(b => b.UseSharedDatabases())
        {
            string tenantRoot = Path.Combine(TestProcessBootstrap.SharedCustomizePath, CustomizeId);
            WriteLanguageOverride(tenantRoot);
            WriteProgramSettingsOverride(tenantRoot);
            WriteFormLayoutOverride(tenantRoot);
        }

        private static void WriteLanguageOverride(string tenantRoot)
        {
            var resource = new LanguageResource { Namespace = ProgId, Lang = Lang };
            resource.Items.Add(FormSchemaLocalizer.SchemaDisplayNameKey, OverriddenSchemaDisplayName);
            resource.Items.Add(
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    FormSchemaLocalizer.FieldCaptionKeyFormat, OverriddenField),
                OverriddenCaption);

            string dir = Path.Combine(tenantRoot, "Language", Lang);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"{ProgId}.Language.xml"), XmlCodec.Serialize(resource));
        }

        private static void WriteProgramSettingsOverride(string tenantRoot)
        {
            var settings = new ProgramSettings();
            var category = new ProgramCategory("customized", "Customized");
            category.Items!.Add(new ProgramItem(ProgId, "Customers")
            {
                BusinessObject = OverriddenBusinessObject
            });
            settings.Categories!.Add(category);

            Directory.CreateDirectory(tenantRoot);
            File.WriteAllText(Path.Combine(tenantRoot, "ProgramSettings.xml"), XmlCodec.Serialize(settings));
        }

        /// <summary>
        /// Writes the tenant's layout. Captions are left empty on purpose: a layout file describes
        /// structure only, and whatever text ends up on screen must have come from the localized
        /// schema, not from here.
        /// </summary>
        private static void WriteFormLayoutOverride(string tenantRoot)
        {
            var section = new LayoutSection { Name = "Tenant" };
            section.Fields!.Add(new LayoutField { FieldName = "sys_id" });
            section.Fields.Add(new LayoutField { FieldName = OverriddenField });

            var layout = new FormLayout { LayoutId = ProgId, ProgId = ProgId };
            layout.Sections!.Add(section);

            string dir = Path.Combine(tenantRoot, "FormLayout");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"{ProgId}.FormLayout.xml"), XmlCodec.Serialize(layout));
        }
    }
}
