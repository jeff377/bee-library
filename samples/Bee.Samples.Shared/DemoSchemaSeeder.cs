using System.Globalization;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Definition.Storage;

namespace Bee.Samples.Shared;

/// <summary>
/// Process-once helper that auto-creates the demo's Employee tables and seeds two
/// rows so the Blazor demo list view is not empty on first run. Idempotent: a second
/// invocation is a no-op once schema + rows are in place.
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

    public static void EnsureSchemaAndSeed(IDefineAccess defineAccess, IDbConnectionManager connectionManager, IDbAccessFactory dbAccessFactory)
    {
        ArgumentNullException.ThrowIfNull(defineAccess);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(dbAccessFactory);

        EnsureSchema(defineAccess, connectionManager);
        SeedEmployees(dbAccessFactory);
        SeedDepartments(dbAccessFactory);
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
        // Framework table polled by CacheNotifyPoller; schema materialized from
        // Bee.Definition embedded defaults by DemoBackend.AddBeeBackend.
        builder.Execute("common", CacheNotifyTable);
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
