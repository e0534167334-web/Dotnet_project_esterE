using Baby_Clinic.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Baby_Clinic.Tests.Concurrency;

// =====================================================================
// שלב 7 – הוכחת הטיפול בתחרות: בסיס נתונים ייעודי לבדיקות
// ---------------------------------------------------------------------
// InMemory ו-SQLite לא תומכים ב-xmin כ-concurrency token, ולכן הבדיקה רצה
// מול PostgreSQL אמיתי - בבסיס נפרד מבסיס הפיתוח.
//
// ה-connection string לא כתוב בקוד. הוא נקרא מאחד מהשניים:
//   1. User Secrets של פרויקט הבדיקות:
//      dotnet user-secrets set "ConnectionStrings:TestDb" "Host=localhost;Port=5432;Database=baby_clinic_tests;Username=postgres;Password=..." --project Baby_Clinic.Tests
//   2. משתנה סביבה: ConnectionStrings__TestDb
//
// הבסיס נמחק ונוצר מחדש (migrations) בתחילת הריצה, ונמחק בסופה.
// =====================================================================
public class TestDatabaseFixture : IAsyncLifetime
{
    private const string ConnectionStringName = "TestDb";

    public string ConnectionString { get; }

    public TestDatabaseFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TestDatabaseFixture>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Set it in the User Secrets of Baby_Clinic.Tests " +
                $"or in the environment variable ConnectionStrings__{ConnectionStringName} (see README).");

        // הגנה: הבדיקה מוחקת את הבסיס, ולכן מסרבים לרוץ על בסיס שלא נראה כמו בסיס בדיקות
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (database is null || !database.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to run against database '{database}': the test database name must contain 'test'.");

        ConnectionString = connectionString;
    }

    // כל בדיקה מקבלת מופע AppDbContext חדש ונפרד - בדיוק כמו שתי בקשות HTTP שונות
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
