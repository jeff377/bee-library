using System.Globalization;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Storage;

namespace Bee.Samples.Shared;

/// <summary>
/// Process-once helper that auto-creates the demo's Employee tables plus the framework
/// tables the Login path needs, and seeds rows so the Blazor demo list view is not empty
/// on first run. Idempotent: a second invocation is a no-op once schema + rows are in place.
/// </summary>
/// <remarks>
/// Reads schema definitions through <see cref="IDefineAccess"/> (which the Blazor host
/// has already wired through <c>AddBeeFramework</c>) and writes through
/// <see cref="IDbAccessFactory"/>. SQLite is the only target — adding other databases
/// would need engine-specific UUID literals.
/// </remarks>
public static class DemoSchemaSeeder
{
    private const string DatabaseId = "common";
    private const string EmployeeTable = "ft_employee";
    private const string EmployeePhoneTable = "ft_employee_phone";
    private const string DepartmentTable = "ft_department";
    private const string ProjectTable = "ft_project";
    private const string ProjectMemberTable = "ft_project_member";
    private const string CacheNotifyTable = "st_cache_notify";
    private const string SessionTable = "st_session";
    private const string UserTable = "st_user";

    public static void EnsureSchemaAndSeed(IDefineAccess defineAccess, IDbConnectionManager connectionManager, IDbAccessFactory dbAccessFactory)
    {
        ArgumentNullException.ThrowIfNull(defineAccess);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(dbAccessFactory);

        EnsureSchema(defineAccess, connectionManager);
        SeedEmployees(dbAccessFactory);
        SeedDepartments(dbAccessFactory);
        SeedDemoUser(dbAccessFactory);
    }

    private static void EnsureSchema(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
    {
        var builder = new TableSchemaBuilder(DatabaseId, defineAccess, connectionManager);
        builder.Execute("common", EmployeeTable);
        builder.Execute("common", EmployeePhoneTable);
        // Lookup demo tables: Department is the lookup source, Project carries the
        // relation fields (master lookup + in-cell detail lookup).
        builder.Execute("common", DepartmentTable);
        builder.Execute("common", ProjectTable);
        builder.Execute("common", ProjectMemberTable);
        // Framework tables, all materialized from Bee.Definition embedded defaults by
        // DemoBackend.AddBeeBackend. st_cache_notify is polled by CacheNotifyPoller;
        // st_session and st_user are both on the Login path — overriding authentication
        // avoids stored credentials, not the session seed or the user's locale row.
        builder.Execute("common", CacheNotifyTable);
        builder.Execute("common", SessionTable);
        builder.Execute("common", UserTable);
    }

    private static void SeedEmployees(IDbAccessFactory dbAccessFactory)
    {
        var dbAccess = dbAccessFactory.Create(DatabaseId);

        var countSpec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT COUNT(*) FROM {EmployeeTable}");
        var count = Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture);
        if (count > 0) return;

        InsertEmployee(dbAccess, "E001", "Alice Chen",   new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true);
        InsertEmployee(dbAccess, "E002", "Bob Liu",      new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc), isActive: true);
        InsertEmployee(dbAccess, "E003", "Carol Wang",   new DateTime(2023, 7, 20, 0, 0, 0, DateTimeKind.Utc), isActive: false);
    }

    private static void SeedDepartments(IDbAccessFactory dbAccessFactory)
    {
        var dbAccess = dbAccessFactory.Create(DatabaseId);

        var countSpec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT COUNT(*) FROM {DepartmentTable}");
        var count = Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture);
        if (count > 0) return;

        InsertDepartment(dbAccess, "D001", "Engineering");
        InsertDepartment(dbAccess, "D002", "Sales");
    }

    /// <summary>
    /// Seeds the row <c>Login</c> reads the signing-in user's locale from.
    /// </summary>
    /// <remarks>
    /// Credentials are deliberately not seeded: <see cref="DemoAuthenticatingSystemBusinessObject"/>
    /// authenticates against <see cref="DemoCredentials"/> and never reads this row's password,
    /// which stays blank — and a blank stored hash is rejected outright by
    /// <c>UserRepository.VerifyPassword</c>, so this row cannot be signed in to on its own.
    /// Time zone and culture stay blank too, which is what makes the session fall back to the
    /// deployment-wide defaults in <c>BackendConfiguration</c>.
    /// </remarks>
    private static void SeedDemoUser(IDbAccessFactory dbAccessFactory)
    {
        var dbAccess = dbAccessFactory.Create(DatabaseId);

        var countSpec = new DbCommandSpec(
            DbCommandKind.Scalar,
            $"SELECT COUNT(*) FROM {UserTable} WHERE sys_id = {{0}}",
            DemoCredentials.UserId);
        var count = Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture);
        if (count > 0) return;

        var spec = new DbCommandSpec(
            DbCommandKind.NonQuery,
            $"INSERT INTO {UserTable} (sys_rowid, sys_id, sys_name, password, time_zone, culture, sys_insert_time) " +
            "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
            Guid.NewGuid(), DemoCredentials.UserId, DemoCredentials.DisplayName,
            string.Empty, string.Empty, string.Empty, DateTime.UtcNow);
        dbAccess.Execute(spec);
    }

    private static void InsertDepartment(DbAccess dbAccess, string sysId, string name)
    {
        var spec = new DbCommandSpec(
            DbCommandKind.NonQuery,
            $"INSERT INTO {DepartmentTable} (sys_rowid, sys_id, sys_name) VALUES ({{0}}, {{1}}, {{2}})",
            Guid.NewGuid(), sysId, name);
        dbAccess.Execute(spec);
    }

    private static void InsertEmployee(DbAccess dbAccess, string sysId, string name, DateTime hireDate, bool isActive)
    {
        var spec = new DbCommandSpec(
            DbCommandKind.NonQuery,
            $"INSERT INTO {EmployeeTable} (sys_rowid, sys_id, sys_name, hire_date, is_active) " +
            "VALUES ({0}, {1}, {2}, {3}, {4})",
            Guid.NewGuid(), sysId, name, hireDate, isActive ? 1 : 0);
        dbAccess.Execute(spec);
    }
}
