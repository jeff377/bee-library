namespace Bee.Tests.Shared
{
    /// <summary>
    /// Marks a parameterized test that requires a test database connection.
    /// When the <c>BEE_TEST_DB_CONNSTR</c> environment variable is not set, the test is skipped.
    /// This covers both local (no <c>.runsettings</c>) and CI scenarios without DB infrastructure.
    /// </summary>
    public class DbTheoryAttribute : TheoryAttribute
    {
        public DbTheoryAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEE_TEST_DB_CONNSTR")))
                Skip = "Skipped – requires BEE_TEST_DB_CONNSTR (test database)";
        }
    }
}
