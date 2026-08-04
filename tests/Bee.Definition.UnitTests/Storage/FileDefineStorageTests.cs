using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// FileDefineStorage 讀寫 XML 檔案的行為測試。
    /// 各測試使用隔離的臨時目錄做為 DefinePath（透過 <c>WithTempDefinePath</c>），
    /// 不操弄 <see cref="DefinePathInfo"/> 等 process-wide static，可與其他 test class 平行執行。
    /// </summary>
    public class FileDefineStorageTests
    {
        [Fact]
        [DisplayName("SaveFormSchema / GetFormSchema 應可寫入後讀回相同結構")]
        public void SaveAndGetFormSchema_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var schema = new FormSchema("Demo", "示範");
                var table = schema.Tables!.Add("Demo", "示範");
                table.Fields!.Add("sys_id", "編號", FieldDbType.String);

                // Act
                storage.SaveFormSchema(schema);
                var restored = storage.GetFormSchema("Demo");

                // Assert
                Assert.NotNull(restored);
                Assert.Equal("Demo", restored!.ProgId);
                Assert.Equal("示範", restored.DisplayName);
            });
        }

        [Fact]
        [DisplayName("GetFormSchema 檔案不存在應拋出 FileNotFoundException")]
        public void GetFormSchema_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetFormSchema("nonexistent"));
            });
        }

        [Fact]
        [DisplayName("SaveTableSchema / GetTableSchema 應可寫入後讀回")]
        public void SaveAndGetTableSchema_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var schema = new TableSchema { TableName = "ft_demo", DisplayName = "Demo" };
                schema.Fields!.Add("sys_no", "流水號", FieldDbType.AutoIncrement);

                // Act
                storage.SaveTableSchema("common", schema);
                var restored = storage.GetTableSchema("common", "ft_demo");

                // Assert
                Assert.NotNull(restored);
                Assert.Equal("ft_demo", restored!.TableName);
                Assert.Single(restored.Fields!);
            });
        }

        [Fact]
        [DisplayName("GetTableSchema 檔案不存在應拋出 FileNotFoundException")]
        public void GetTableSchema_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetTableSchema("common", "missing"));
            });
        }

        [Fact]
        [DisplayName("SaveFormLayout / GetFormLayout 應可寫入後讀回")]
        public void SaveAndGetFormLayout_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var layout = new FormLayout { LayoutId = "DemoLayout", Caption = "示範" };

                // Act
                storage.SaveFormLayout(layout);
                var restored = storage.GetFormLayout("DemoLayout");

                // Assert
                Assert.NotNull(restored);
                Assert.Equal("DemoLayout", restored!.LayoutId);
            });
        }

        [Fact]
        [DisplayName("GetFormLayout 檔案不存在應回 null（履行介面宣告的 nullable 契約）")]
        public void GetFormLayout_FileNotFound_ReturnsNull()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert —— 缺 layout 檔是正常情境（框架改以 FormSchema 生成），不是錯誤
                Assert.Null(storage.GetFormLayout("missing"));
            });
        }

        [Fact]
        [DisplayName("SaveDbCategorySettings / GetDbCategorySettings 應可寫入後讀回")]
        public void SaveAndGetDbCategorySettings_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var settings = new DbCategorySettings();

                // Act
                storage.SaveDbCategorySettings(settings);
                var restored = storage.GetDbCategorySettings();

                // Assert
                Assert.NotNull(restored);
            });
        }

        [Fact]
        [DisplayName("GetDbCategorySettings 檔案不存在應拋出 FileNotFoundException")]
        public void GetDbCategorySettings_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetDbCategorySettings());
            });
        }

        [Fact]
        [DisplayName("SaveProgramSettings / GetProgramSettings 應可寫入後讀回")]
        public void SaveAndGetProgramSettings_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                var settings = new ProgramSettings();

                storage.SaveProgramSettings(settings);
                var restored = storage.GetProgramSettings();

                Assert.NotNull(restored);
            });
        }

        [Fact]
        [DisplayName("GetProgramSettings 檔案不存在應拋出 FileNotFoundException")]
        public void GetProgramSettings_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                Assert.Throws<FileNotFoundException>(() => storage.GetProgramSettings());
            });
        }

        [Fact]
        [DisplayName("SaveLanguage / GetLanguage 應可寫入後讀回相同語言資源")]
        public void SaveAndGetLanguage_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                var resource = new LanguageResource { Lang = "en", Namespace = "Core" };

                storage.SaveLanguage(resource);
                var restored = storage.GetLanguage("en", "Core");

                Assert.NotNull(restored);
                Assert.Equal("en", restored!.Lang);
                Assert.Equal("Core", restored.Namespace);
            });
        }

        [Fact]
        [DisplayName("GetLanguage 檔案不存在應回傳 null（非拋例外）")]
        public void GetLanguage_FileNotFound_ReturnsNull()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                var result = storage.GetLanguage("zh-TW", "Missing");
                Assert.Null(result);
            });
        }

        [Fact]
        [DisplayName("SaveMenuSettings / GetMenuSettings 應可寫入後讀回巢狀結構")]
        public void SaveAndGetMenuSettings_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var settings = new MenuSettings();
                var folder = settings.Items!.AddFolder("sales", "銷售");
                folder.Items!.AddEntry("sales-order", "Order", "訂單");

                // Act
                storage.SaveMenuSettings(settings);
                var restored = storage.GetMenuSettings();

                // Assert
                Assert.NotNull(restored);
                var restoredFolder = Assert.IsType<MenuFolder>(restored!.Items!.Single());
                var entry = Assert.IsType<MenuEntry>(restoredFolder.Items!.Single());
                Assert.Equal("Order", entry.ProgId);
            });
        }

        [Fact]
        [DisplayName("MenuSettings.xml 不存在時 GetMenuSettings 應回傳 null（無選單的部署屬正常）")]
        public void GetMenuSettings_FileMissing_ReturnsNull()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);

                Assert.Null(storage.GetMenuSettings());
            });
        }

        [Fact]
        [DisplayName("MenuSettings 全樹 Id 重複時 GetMenuSettings 應於載入期拋出")]
        public void GetMenuSettings_DuplicateIdAcrossTree_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Written by hand: the collection would reject the duplicate if it were built in
                // memory, which is precisely why the tree walk has to run at load time.
                File.WriteAllText(paths.GetMenuSettingsFilePath(), """
                    <?xml version="1.0" encoding="utf-8"?>
                    <MenuSettings>
                      <Items>
                        <MenuFolder Id="dup" Caption="資料夾">
                          <Items><MenuEntry Id="dup" ProgId="Order" Caption="訂單" /></Items>
                        </MenuFolder>
                      </Items>
                    </MenuSettings>
                    """);
                var storage = new FileDefineStorage(paths);

                var ex = Assert.Throws<InvalidOperationException>(() => storage.GetMenuSettings());
                Assert.Contains("'dup'", ex.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        [DisplayName("舊版巢狀 ProgramSettings.xml 應於載入期拋出並指向遷移命令，而非靜默讀成空註冊表")]
        public void GetProgramSettings_LegacyLayout_ThrowsPointingAtMigration()
        {
            WithTempDefinePath(paths =>
            {
                File.WriteAllText(paths.GetProgramSettingsFilePath(), """
                    <?xml version="1.0" encoding="utf-8"?>
                    <ProgramSettings>
                      <Categories>
                        <ProgramCategory Id="master-data" DisplayName="主檔">
                          <Items><ProgramItem ProgId="Customer" DisplayName="客戶" /></Items>
                        </ProgramCategory>
                      </Categories>
                    </ProgramSettings>
                    """);
                var storage = new FileDefineStorage(paths);

                var ex = Assert.Throws<NotSupportedException>(() => storage.GetProgramSettings());
                Assert.Contains("split-menu", ex.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        [DisplayName("攤平後的註冊表項目（含 BusinessObject）應可寫入後讀回")]
        public void SaveAndGetProgramSettings_FlatItems_RoundTrip()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                var settings = new ProgramSettings();
                settings.Items!.Add("Order", "訂單").BusinessObject = "MyErp.OrderBO, MyErp";

                storage.SaveProgramSettings(settings);
                var restored = storage.GetProgramSettings();

                Assert.NotNull(restored);
                Assert.Equal("MyErp.OrderBO, MyErp", restored!.Items!["Order"].BusinessObject);
            });
        }

        /// <summary>
        /// 建立新的臨時目錄並把對應的 <see cref="PathOptions"/> 傳給 <paramref name="action"/>，
        /// 測試結束後刪除目錄。Tests inject the supplied <see cref="PathOptions"/> directly into
        /// <see cref="FileDefineStorage"/> rather than relying on the shared
        /// <see cref="DefinePathInfo"/> static facade.
        /// </summary>
        private static void WithTempDefinePath(Action<PathOptions> action)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-define-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                action(new PathOptions { DefinePath = tempDir });
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
