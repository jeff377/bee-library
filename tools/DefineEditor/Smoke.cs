using Bee.Base.Data;
using Bee.Definition.Collections;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Sorting;
using Bee.DefineEditor.Services;
using Bee.DefineEditor.ViewModels;

namespace Bee.DefineEditor;

/// <summary>
/// Headless smoke harness driven by <c>dotnet run -- --smoke &lt;fixture-path&gt;</c>.
/// Runs (1) the FormSchema flow against the supplied fixture, then (2) round-trip
/// checks on all four singleton editors built from fresh in-memory instances so
/// the smoke does not depend on extra fixture files.
/// </summary>
internal static class Smoke
{
    public static int Run(string fixturePath)
    {
        var formSchemaResult = RunFormSchemaSmoke(fixturePath);
        if (formSchemaResult != 0) return formSchemaResult;

        var singletonResult = RunSingletonSmoke();
        if (singletonResult != 0) return singletonResult;

        Console.WriteLine("[smoke] OK — FormSchema + 8 multi-instance editors + ConnectionStringParser + tab commands all green.");
        return 0;
    }

    private static int RunFormSchemaSmoke(string fixturePath)
    {
        if (!File.Exists(fixturePath))
        {
            Console.Error.WriteLine($"[smoke] fixture not found: {fixturePath}");
            return 2;
        }

        var tempDir = MakeTempDir("formschema");
        var target = Path.Combine(tempDir, Path.GetFileName(fixturePath));
        File.Copy(fixturePath, target);
        Console.WriteLine($"[smoke:formschema] copy → {target}");

        try
        {
            var sentinelCaption = $"SMOKE_{Guid.NewGuid():N}".Substring(0, 16);
            var newFieldName = $"smoke_field_{Guid.NewGuid():N}".Substring(0, 24);
            var relationFieldName = $"smoke_rel_{Guid.NewGuid():N}".Substring(0, 22);
            const string targetProgId = "Department";

            var solution = new SolutionContext(new[] { "Employee", "Department" });
            var vm = FormSchemaDocumentViewModel.Load(target, solution);
            var master = vm.Schema.MasterTable
                ?? throw new InvalidOperationException("fixture has no master table");
            var firstField = master.Fields!.First();
            var originalFirstFieldName = firstField.FieldName;
            firstField.Caption = sentinelCaption;

            master.Fields!.Add(new FormField(newFieldName, "Smoke New", FieldDbType.String));

            var relField = new FormField(relationFieldName, "Smoke Relation", FieldDbType.Guid)
            {
                RelationProgId = targetProgId,
            };
            relField.RelationFieldMappings!.Add(new FieldMapping("sys_id", originalFirstFieldName));
            master.Fields!.Add(relField);

            vm.SaveCommand.Execute(null);
            if (vm.IsDirty) return Fail(31, "FormSchema IsDirty still true after save");

            var vm2 = FormSchemaDocumentViewModel.Load(target, solution);
            var master2 = vm2.Schema.MasterTable!;
            var rtFirst = master2.Fields!.FirstOrDefault(f => f.FieldName == originalFirstFieldName);
            if (rtFirst is null || rtFirst.Caption != sentinelCaption)
                return Fail(32, "FormSchema caption round-trip failed");
            var rtPlain = master2.Fields!.FirstOrDefault(f => f.FieldName == newFieldName);
            if (rtPlain is null || rtPlain.Caption != "Smoke New")
                return Fail(33, "FormSchema new field missing after reload");
            var rtRel = master2.Fields!.FirstOrDefault(f => f.FieldName == relationFieldName);
            if (rtRel is null
                || rtRel.RelationProgId != targetProgId
                || rtRel.RelationFieldMappings is not { Count: 1 } mappings
                || mappings[0].SourceField != "sys_id"
                || mappings[0].DestinationField != originalFirstFieldName)
                return Fail(34, "FormSchema relation mapping round-trip failed");

            vm2.ValidateCommand.Execute(null);
            var errors = vm2.Issues.Count(i => i.Severity == Models.ValidationSeverity.Error);
            if (errors != 0) return Fail(35, $"FormSchema validation has {errors} errors");

            Console.WriteLine("[smoke:formschema] OK");
            return 0;
        }
        finally { TryDelete(tempDir); }
    }

    private static int RunSingletonSmoke()
    {
        // Short-circuit on first failure: returning the failing code preserves
        // its identity for the exit status. Summing as before would let two
        // small fail codes (e.g. 31 + 52) overlap with a single legitimate
        // one (e.g. 83), confusing the post-mortem.
        var phases = new Func<int>[]
        {
            RunPermissionModelsSmoke,
            RunDbCategorySettingsSmoke,
            RunProgramSettingsSmoke,
            RunSystemSettingsSmoke,
            RunDatabaseSettingsSmoke,
            RunConnectionStringParserSmoke,
            RunTableSchemaSmoke,
            RunFormLayoutSmoke,
            RunLanguageSmoke,
            RunTabCommandsSmoke,
        };
        foreach (var phase in phases)
        {
            var code = phase();
            if (code != 0) return code;
        }
        return 0;
    }

    private static int RunPermissionModelsSmoke()
    {
        var tempDir = MakeTempDir("perm");
        var target = Path.Combine(tempDir, "PermissionModels.xml");
        try
        {
            var root = new PermissionModels();
            var model = new PermissionModel("PurchaseOrder", "採購單");
            model.Rules!.Add(new PermissionRule(PermissionAction.Read, ScopeStrategy.Dept));
            model.Rules!.Add(new PermissionRule(PermissionAction.Create));
            root.Models!.Add(model);
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = PermissionModelsDocumentViewModel.Load(target);
            var loaded = vm.Root.Models!.FirstOrDefault(m => m.ModelId == "PurchaseOrder");
            if (loaded is null) return Fail(41, "PermissionModel missing after reload");
            if (loaded.Rules?.Count != 2) return Fail(42, "PermissionRule count mismatch");
            var readRule = loaded.Rules.FirstOrDefault(r => r.Action == PermissionAction.Read);
            if (readRule?.Scope != ScopeStrategy.Dept)
                return Fail(43, $"Read rule Scope expected Dept got {readRule?.Scope}");

            // Mutate via editor command path and save through SaveCommand.
            vm.SelectedTreeNode = vm.Roots[0]; // root
            vm.AddModelCommand.Execute(null);
            var newModel = vm.Root.Models!.Last();
            newModel.DisplayName = "SmokeNew";
            vm.SaveCommand.Execute(null);

            var vm2 = PermissionModelsDocumentViewModel.Load(target);
            if (vm2.Root.Models!.LastOrDefault()?.DisplayName != "SmokeNew")
                return Fail(44, "New PermissionModel did not round-trip");

            vm2.ValidateCommand.Execute(null);
            Console.WriteLine($"[smoke:permission] OK ({vm2.Issues.Count} non-error issues)");
            return 0;
        }
        catch (Exception ex) { return Fail(45, $"PermissionModels smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static int RunDbCategorySettingsSmoke()
    {
        var tempDir = MakeTempDir("db");
        var target = Path.Combine(tempDir, "DbCategorySettings.xml");
        try
        {
            var root = new DbCategorySettings();
            var category = new DbCategory { Id = "common", DisplayName = "通用資料庫" };
            category.Tables!.Add(new TableItem { TableName = "st_user", DisplayName = "使用者" });
            root.Categories!.Add(category);
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = DbCategorySettingsDocumentViewModel.Load(target);
            var loaded = vm.Root.Categories!.FirstOrDefault(c => c.Id == "common");
            if (loaded is null) return Fail(51, "DbCategory missing after reload");
            if (loaded.Tables?.FirstOrDefault(t => t.TableName == "st_user")?.DisplayName != "使用者")
                return Fail(52, "TableItem round-trip failed");

            vm.SelectedTreeNode = vm.Roots[0];
            vm.AddCategoryCommand.Execute(null);
            var newCat = vm.Root.Categories!.Last();
            newCat.DisplayName = "SmokeNew";
            vm.SaveCommand.Execute(null);

            var vm2 = DbCategorySettingsDocumentViewModel.Load(target);
            if (vm2.Root.Categories!.LastOrDefault()?.DisplayName != "SmokeNew")
                return Fail(53, "New DbCategory did not round-trip");

            vm2.ValidateCommand.Execute(null);
            Console.WriteLine($"[smoke:db] OK ({vm2.Issues.Count} non-error issues)");
            return 0;
        }
        catch (Exception ex) { return Fail(54, $"DbCategorySettings smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static int RunProgramSettingsSmoke()
    {
        var tempDir = MakeTempDir("prog");
        var target = Path.Combine(tempDir, "ProgramSettings.xml");
        try
        {
            var root = new ProgramSettings();
            var cat = new ProgramCategory("hr", "人事");
            cat.Items!.Add(new ProgramItem { ProgId = "Employee", DisplayName = "員工", BusinessObject = "EmployeeBO" });
            root.Categories!.Add(cat);
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = ProgramSettingsDocumentViewModel.Load(target);
            var loadedCat = vm.Root.Categories!.FirstOrDefault(c => c.Id == "hr");
            if (loadedCat is null) return Fail(61, "ProgramCategory missing after reload");
            var loadedProg = loadedCat.Items?.FirstOrDefault(p => p.ProgId == "Employee");
            if (loadedProg?.BusinessObject != "EmployeeBO")
                return Fail(62, $"ProgramItem.BusinessObject expected 'EmployeeBO' got '{loadedProg?.BusinessObject}'");

            vm.SelectedTreeNode = vm.Roots[0];
            vm.AddCategoryCommand.Execute(null);
            var newCat = vm.Root.Categories!.Last();
            newCat.DisplayName = "SmokeNew";
            vm.SaveCommand.Execute(null);

            var vm2 = ProgramSettingsDocumentViewModel.Load(target);
            if (vm2.Root.Categories!.LastOrDefault()?.DisplayName != "SmokeNew")
                return Fail(63, "New ProgramCategory did not round-trip");

            vm2.ValidateCommand.Execute(null);
            Console.WriteLine($"[smoke:program] OK ({vm2.Issues.Count} non-error issues)");
            return 0;
        }
        catch (Exception ex) { return Fail(64, $"ProgramSettings smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static int RunSystemSettingsSmoke()
    {
        var tempDir = MakeTempDir("sys");
        var target = Path.Combine(tempDir, "SystemSettings.xml");
        try
        {
            var root = new SystemSettings();
            root.CommonConfiguration.Version = "1.0.0-smoke";
            root.CommonConfiguration.IsDebugMode = true;
            root.CommonConfiguration.DefaultLang = "zh-TW";
            root.BackendConfiguration.CacheNotifyOptions.IntervalSeconds = 42;
            root.BackendConfiguration.SecurityKeySettings.ApiEncryptionKey = "smoke-api-key";
            root.ExtendedProperties!.Add(new Property { Name = "SmokeProp", Value = "SmokeValue" });
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = SystemSettingsDocumentViewModel.Load(target);
            if (vm.Root.CommonConfiguration.Version != "1.0.0-smoke")
                return Fail(71, "CommonConfiguration.Version round-trip failed");
            if (vm.Root.BackendConfiguration.CacheNotifyOptions.IntervalSeconds != 42)
                return Fail(72, "CacheNotifyOptions.IntervalSeconds round-trip failed");
            if (vm.Root.BackendConfiguration.SecurityKeySettings.ApiEncryptionKey != "smoke-api-key")
                return Fail(73, "SecurityKeySettings round-trip failed");
            if (vm.Root.ExtendedProperties!.FirstOrDefault(p => p.Name == "SmokeProp")?.Value != "SmokeValue")
                return Fail(74, "ExtendedProperties round-trip failed");

            // Add ExtendedProperty via command, save, reload, assert.
            var extGroup = vm.Roots[0].Children.Last();
            vm.SelectedTreeNode = extGroup;
            vm.AddPropertyCommand.Execute(null);
            var newProp = vm.Root.ExtendedProperties!.Last();
            newProp.Value = "AddedValue";
            vm.SaveCommand.Execute(null);

            var vm2 = SystemSettingsDocumentViewModel.Load(target);
            if (vm2.Root.ExtendedProperties!.LastOrDefault()?.Value != "AddedValue")
                return Fail(75, "Added ExtendedProperty did not round-trip");

            Console.WriteLine("[smoke:system] OK");
            return 0;
        }
        catch (Exception ex) { return Fail(76, $"SystemSettings smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static int RunDatabaseSettingsSmoke()
    {
        var tempDir = MakeTempDir("db-settings");
        var target = Path.Combine(tempDir, "DatabaseSettings.xml");
        try
        {
            var root = new DatabaseSettings();
            root.Servers!.Add(new DatabaseServer
            {
                Id = "common-server",
                DisplayName = "通用伺服器",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Data Source=localhost;Initial Catalog={@DbName};User ID={@UserId};Password={@Password}",
                UserId = "sa",
                Password = "smoke-pwd",
            });
            root.Items!.Add(new DatabaseItem
            {
                Id = "common",
                CategoryId = "common",
                DisplayName = "通用 DB",
                DatabaseType = DatabaseType.SQLServer,
                ServerId = "common-server",
                DbName = "common",
            });
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = DatabaseSettingsDocumentViewModel.Load(target);
            var server = vm.Root.Servers!.FirstOrDefault(s => s.Id == "common-server");
            if (server is null) return Fail(81, "DatabaseServer missing after reload");
            if (server.UserId != "sa" || server.Password != "smoke-pwd")
                return Fail(82, "Server credentials round-trip failed");
            var item = vm.Root.Items!.FirstOrDefault(i => i.Id == "common");
            if (item?.ServerId != "common-server" || item?.DbName != "common")
                return Fail(83, "Item round-trip failed");

            // Validate — no errors expected for this clean config.
            vm.ValidateCommand.Execute(null);
            var errors = vm.Issues.Count(i => i.Severity == Models.ValidationSeverity.Error);
            if (errors != 0)
            {
                Console.Error.WriteLine("[smoke:db-settings] unexpected validation errors:");
                foreach (var issue in vm.Issues.Where(i => i.Severity == Models.ValidationSeverity.Error))
                    Console.Error.WriteLine($"  - {issue.Path}: {issue.Message}");
                return Fail(84, $"Validator reported {errors} errors");
            }

            // Add via command, save, reload.
            vm.SelectedTreeNode = vm.Roots[0].Children.First(c => c.Kind == DatabaseSettingsDocumentViewModel.KindServersGroup);
            vm.AddServerCommand.Execute(null);
            vm.Root.Servers!.Last().DisplayName = "SmokeNew";
            vm.SaveCommand.Execute(null);

            var vm2 = DatabaseSettingsDocumentViewModel.Load(target);
            if (vm2.Root.Servers!.LastOrDefault()?.DisplayName != "SmokeNew")
                return Fail(85, "Added Server did not round-trip");

            Console.WriteLine("[smoke:db-settings] OK");
            return 0;
        }
        catch (Exception ex) { return Fail(86, $"DatabaseSettings smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static int RunConnectionStringParserSmoke()
    {
        // SQL Server — standard tokens with credentials + database
        var sql = "Data Source=localhost,1433;Initial Catalog=AdventureWorks;User ID=sa;Password=P@ssw0rd!;Encrypt=True";
        var sqlResult = ConnectionStringParser.Parse(sql, DatabaseType.SQLServer);
        if (sqlResult.UserId != "sa") return Fail(91, $"SQLServer UserId expected 'sa' got '{sqlResult.UserId}'");
        if (sqlResult.Password != "P@ssw0rd!") return Fail(92, $"SQLServer Password mismatch '{sqlResult.Password}'");
        if (sqlResult.DbName != "AdventureWorks") return Fail(93, $"SQLServer DbName mismatch '{sqlResult.DbName}'");
        if (!sqlResult.RewrittenConnectionString.Contains("{@UserId}", StringComparison.Ordinal)
            || !sqlResult.RewrittenConnectionString.Contains("{@Password}", StringComparison.Ordinal)
            || !sqlResult.RewrittenConnectionString.Contains("{@DbName}", StringComparison.Ordinal))
            return Fail(94, $"SQLServer placeholders missing — rewritten: {sqlResult.RewrittenConnectionString}");

        // PostgreSQL — different token names
        var pg = "Host=db.example.com;Port=5432;Database=appdb;Username=app;Password=pgpwd;SSL Mode=Require";
        var pgResult = ConnectionStringParser.Parse(pg, DatabaseType.PostgreSQL);
        if (pgResult.UserId != "app") return Fail(95, $"PostgreSQL UserId expected 'app' got '{pgResult.UserId}'");
        if (pgResult.Password != "pgpwd") return Fail(96, $"PostgreSQL Password mismatch '{pgResult.Password}'");
        if (pgResult.DbName != "appdb") return Fail(97, $"PostgreSQL DbName mismatch '{pgResult.DbName}'");

        // Round-trip via Compose — should yield a connection string with the original values back
        var composed = ConnectionStringParser.Compose(pgResult.RewrittenConnectionString,
            pgResult.UserId, pgResult.Password, pgResult.DbName);
        var pgRound = ConnectionStringParser.Parse(composed, DatabaseType.PostgreSQL);
        if (pgRound.UserId != "app" || pgRound.Password != "pgpwd" || pgRound.DbName != "appdb")
            return Fail(98, "PostgreSQL round-trip via Compose failed");

        // Dialect mismatch warning — paste a PG string but tell parser it's SQL Server
        var mismatch = ConnectionStringParser.Parse(pg, DatabaseType.SQLServer);
        if (!mismatch.Warnings.Any(w => w.Contains("not typical of", StringComparison.Ordinal)))
            return Fail(99, "Dialect mismatch warning not raised");

        Console.WriteLine("[smoke:parser] OK (SQL Server + PostgreSQL + dialect-mismatch warning)");
        return 0;
    }

    private static int RunTableSchemaSmoke()
    {
        var tempDir = MakeTempDir("table-schema");
        var target = Path.Combine(tempDir, "Employee.TableSchema.xml");
        try
        {
            var root = new TableSchema { TableName = "st_employee", DisplayName = "員工" };
            root.Fields!.Add(new DbField("sys_no", "流水號", FieldDbType.AutoIncrement));
            root.Fields!.Add(new DbField("sys_id", "員工編號", FieldDbType.String) { Length = 30 });
            var pk = new DbTableIndex { Name = "PK_employee", PrimaryKey = true, Unique = true };
            pk.IndexFields!.Add(new IndexField("sys_no", SortDirection.Asc));
            root.Indexes!.Add(pk);
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = TableSchemaDocumentViewModel.Load(target);
            if (vm.Root.Fields!.FirstOrDefault(f => f.FieldName == "sys_id")?.Length != 30)
                return Fail(101, "DbField.Length round-trip failed");
            if (vm.Root.Indexes!.FirstOrDefault(i => i.Name == "PK_employee")?.PrimaryKey != true)
                return Fail(102, "DbTableIndex.PrimaryKey round-trip failed");

            vm.ValidateCommand.Execute(null);
            var errors = vm.Issues.Count(i => i.Severity == Models.ValidationSeverity.Error);
            if (errors != 0) return Fail(103, $"Validator reported {errors} errors");

            vm.SelectedTreeNode = vm.Roots[0].Children.First(c => c.Kind == TableSchemaDocumentViewModel.KindFieldsGroup);
            vm.AddFieldCommand.Execute(null);
            vm.Root.Fields!.Last().Caption = "SmokeNew";
            vm.SaveCommand.Execute(null);

            var vm2 = TableSchemaDocumentViewModel.Load(target);
            if (vm2.Root.Fields!.LastOrDefault()?.Caption != "SmokeNew")
                return Fail(104, "Added DbField did not round-trip");

            Console.WriteLine("[smoke:table-schema] OK");
            return 0;
        }
        catch (Exception ex) { return Fail(105, $"TableSchema smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static int RunFormLayoutSmoke()
    {
        var tempDir = MakeTempDir("form-layout");
        var target = Path.Combine(tempDir, "default.FormLayout.xml");
        try
        {
            var root = new FormLayout { LayoutId = "default", ProgId = "Employee", Caption = "員工表單", ColumnCount = 2 };
            var section = new LayoutSection { Name = "Header", Caption = "基本資料" };
            section.Fields!.Add(new LayoutField { FieldName = "sys_id", Caption = "編號" });
            section.Fields!.Add(new LayoutField { FieldName = "sys_name", Caption = "姓名", ColumnSpan = 2 });
            root.Sections!.Add(section);
            var grid = new LayoutGrid("Skills", "技能");
            grid.Columns!.Add(new LayoutColumn { FieldName = "skill_code", Caption = "技能代碼", Width = 120 });
            grid.Columns!.Add(new LayoutColumn { FieldName = "skill_level", Caption = "等級", Width = 80 });
            root.Details!.Add(grid);
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = FormLayoutDocumentViewModel.Load(target);
            if (vm.Root.LayoutId != "default" || vm.Root.ProgId != "Employee")
                return Fail(111, "FormLayout root attrs round-trip failed");
            var s = vm.Root.Sections!.FirstOrDefault(x => x.Name == "Header");
            if (s?.Fields!.FirstOrDefault(f => f.FieldName == "sys_name")?.ColumnSpan != 2)
                return Fail(112, "LayoutField.ColumnSpan round-trip failed");
            var g = vm.Root.Details!.FirstOrDefault(x => x.TableName == "Skills");
            if (g?.Columns!.FirstOrDefault(c => c.FieldName == "skill_code")?.Width != 120)
                return Fail(113, "LayoutColumn.Width round-trip failed");

            vm.ValidateCommand.Execute(null);
            if (vm.Issues.Any(i => i.Severity == Models.ValidationSeverity.Error))
                return Fail(114, "Validator unexpectedly reported errors");

            vm.SelectedTreeNode = vm.Roots[0].Children.First(c => c.Kind == FormLayoutDocumentViewModel.KindSectionsGroup);
            vm.AddSectionCommand.Execute(null);
            vm.Root.Sections!.Last().Caption = "SmokeNew";
            vm.SaveCommand.Execute(null);

            var vm2 = FormLayoutDocumentViewModel.Load(target);
            if (vm2.Root.Sections!.LastOrDefault()?.Caption != "SmokeNew")
                return Fail(115, "Added Section did not round-trip");

            Console.WriteLine("[smoke:form-layout] OK");
            return 0;
        }
        catch (Exception ex) { return Fail(116, $"FormLayout smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static int RunLanguageSmoke()
    {
        var tempDir = MakeTempDir("language");
        var target = Path.Combine(tempDir, "Employee.Language.xml");
        try
        {
            var root = new LanguageResource { Namespace = "Employee", Lang = "zh-TW" };
            root.Items.Add(new LanguageItem { Key = "Caption", Value = "員工" });
            root.Items.Add(new LanguageItem { Key = "Field.sys_id.Caption", Value = "編號" });
            var enumDef = new LanguageEnum { Name = "Gender" };
            enumDef.Entries.Add(new LanguageEnumEntry { Code = "M", Text = "男" });
            enumDef.Entries.Add(new LanguageEnumEntry { Code = "F", Text = "女" });
            root.Enums.Add(enumDef);
            Bee.Base.Serialization.XmlCodec.SerializeToFile(root, target);

            var vm = LanguageDocumentViewModel.Load(target);
            if (vm.Root.Namespace != "Employee" || vm.Root.Lang != "zh-TW")
                return Fail(121, "LanguageResource attrs round-trip failed");
            if (vm.Root.Items.FirstOrDefault(i => i.Key == "Caption")?.Value != "員工")
                return Fail(122, "LanguageItem round-trip failed");
            var loadedEnum = vm.Root.Enums.FirstOrDefault(e => e.Name == "Gender");
            if (loadedEnum?.Entries.FirstOrDefault(e => e.Code == "M")?.Text != "男")
                return Fail(123, "LanguageEnumEntry round-trip failed");

            vm.ValidateCommand.Execute(null);
            if (vm.Issues.Any(i => i.Severity == Models.ValidationSeverity.Error))
                return Fail(124, "Validator unexpectedly reported errors");

            vm.SelectedTreeNode = vm.Roots[0].Children.First(c => c.Kind == LanguageDocumentViewModel.KindItemsGroup);
            vm.AddItemCommand.Execute(null);
            vm.Root.Items.Last().Value = "SmokeNew";
            vm.SaveCommand.Execute(null);

            var vm2 = LanguageDocumentViewModel.Load(target);
            if (vm2.Root.Items.LastOrDefault()?.Value != "SmokeNew")
                return Fail(125, "Added LanguageItem did not round-trip");

            Console.WriteLine("[smoke:language] OK");
            return 0;
        }
        catch (Exception ex) { return Fail(126, $"Language smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    /// <summary>
    /// Exercises the shell-level tab commands (Close Others / Close to the
    /// Right / Close Saved / Close All) and the batch Save All directly on
    /// <see cref="MainWindowViewModel"/>, bypassing the tree so the smoke
    /// stays fixture-free.
    /// </summary>
    private static int RunTabCommandsSmoke()
    {
        var tempDir = MakeTempDir("tabs");
        try
        {
            var vm = new MainWindowViewModel();
            PermissionModelsDocumentViewModel Open(string name)
            {
                var path = Path.Combine(tempDir, name);
                Bee.Base.Serialization.XmlCodec.SerializeToFile(new PermissionModels(), path);
                var doc = PermissionModelsDocumentViewModel.Load(path);
                vm.OpenDocuments.Add(doc);
                vm.ActiveDocument = doc;
                return doc;
            }

            // Welcome tab: idempotent (re-invoking activates, not duplicates),
            // closes like a plain tab.
            vm.ShowWelcome();
            vm.ShowWelcome();
            if (vm.OpenDocuments.OfType<WelcomeDocumentViewModel>().Count() != 1)
                return Fail(140, "ShowWelcome should activate the existing tab, not duplicate it");
            vm.CloseAllDocumentsCommand.Execute(null);
            if (vm.OpenDocuments.Count != 0)
                return Fail(141, "Welcome tab should close like a normal tab");

            Open("A.xml");
            var b = Open("B.xml");
            Open("C.xml");
            vm.CloseOtherDocumentsCommand.Execute(b);
            if (vm.OpenDocuments.Count != 1 || vm.OpenDocuments[0] != b || vm.ActiveDocument != b)
                return Fail(131, "CloseOthers should leave only the clicked tab");

            vm.CloseAllDocumentsCommand.Execute(null);
            var a = Open("A.xml");
            Open("B.xml");
            Open("C.xml");
            vm.CloseDocumentsToTheRightCommand.Execute(a);
            if (vm.OpenDocuments.Count != 1 || vm.OpenDocuments[0] != a)
                return Fail(132, "CloseToTheRight should close everything after the clicked tab");

            vm.CloseAllDocumentsCommand.Execute(null);
            a = Open("A.xml");
            Open("B.xml");
            a.IsDirty = true;
            vm.CloseSavedDocumentsCommand.Execute(null);
            if (vm.OpenDocuments.Count != 1 || vm.OpenDocuments[0] != a || vm.ActiveDocument != a)
                return Fail(133, "CloseSaved should keep only dirty tabs");
            if (!vm.HasDirtyDocuments)
                return Fail(134, "HasDirtyDocuments should be true while a dirty tab is open");

            var b2 = Open("B.xml");
            b2.IsDirty = true;
            vm.SaveAllCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            if (a.IsDirty || b2.IsDirty)
                return Fail(135, "SaveAll left documents dirty");
            if (vm.HasDirtyDocuments)
                return Fail(136, "HasDirtyDocuments should be false after SaveAll");

            // Closing a dirty tab headless must skip the unsaved-changes
            // prompt (no owner window) and still proceed with the close.
            var d = Open("D.xml");
            d.IsDirty = true;
            vm.CloseDocumentCommand.Execute(d);
            if (vm.OpenDocuments.Contains(d))
                return Fail(139, "Headless close of a dirty tab should proceed without prompting");

            vm.CloseAllDocumentsCommand.Execute(null);
            if (vm.OpenDocuments.Count != 0 || vm.ActiveDocument is not null)
                return Fail(137, "CloseAll should close every tab");

            Console.WriteLine("[smoke:tabs] OK");
            return 0;
        }
        catch (Exception ex) { return Fail(138, $"Tab commands smoke crashed: {ex.Message}"); }
        finally { TryDelete(tempDir); }
    }

    private static string MakeTempDir(string tag)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"bee-define-editor-smoke-{tag}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDelete(string dir)
    {
        try { Directory.Delete(dir, recursive: true); }
        catch (IOException) { /* best effort */ }
    }

    private static int Fail(int code, string message)
    {
        Console.Error.WriteLine($"[smoke] FAIL({code}) {message}");
        return code;
    }
}
