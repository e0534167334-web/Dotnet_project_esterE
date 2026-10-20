using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baby_Clinic.Data.Seed;

// רץ בעליית השרת (בפיתוח): migrate, ואם הטבלאות ריקות - נתוני דמו
public class DbInitializer
{
    // משתמשי דמו - מתועדים ב-README
    public const string AdminEmail = "admin@babyclinic.local";
    public const string AdminPassword = "Admin123!";
    public const string ParentEmail = "parent@babyclinic.local";
    public const string ParentPassword = "Parent123!";

    private const int SeedDays = 7;
    private static readonly string[] Nurses = { "רחל כהן", "מרים לוי" };

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(AppDbContext context, IPasswordHasher passwordHasher, ILogger<DbInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var initializer = ActivatorUtilities.CreateInstance<DbInitializer>(scope.ServiceProvider);
        await initializer.RunAsync(ct);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await _context.Database.MigrateAsync(ct);

        var visitTypes = await SeedVisitTypesAsync(ct);
        await SeedUsersAsync(ct);
        await SeedSlotsAsync(visitTypes, ct);
    }

    private async Task<List<VisitType>> SeedVisitTypesAsync(CancellationToken ct)
    {
        if (await _context.VisitTypes.AnyAsync(ct))
            return await _context.VisitTypes.ToListAsync(ct);

        var visitTypes = new List<VisitType>
        {
            new() { Name = "חיסון" },
            new() { Name = "בדיקה התפתחותית" },
            new() { Name = "שקילה ומדידה" },
            new() { Name = "ייעוץ הנקה" }
        };

        _context.VisitTypes.AddRange(visitTypes);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} visit types", visitTypes.Count);
        return visitTypes;
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        if (await _context.Users.AnyAsync(ct))
            return;

        var admin = new User
        {
            Email = AdminEmail,
            PasswordHash = _passwordHasher.Hash(AdminPassword),
            Role = Role.Admin
        };

        var parent = new User
        {
            Email = ParentEmail,
            PasswordHash = _passwordHasher.Hash(ParentPassword),
            Role = Role.Parent,
            Children =
            {
                new Child { Name = "נועה", BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)) }
            }
        };

        _context.Users.AddRange(admin, parent);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded demo users (Admin, Parent) and one child");
    }

    private async Task SeedSlotsAsync(List<VisitType> visitTypes, CancellationToken ct)
    {
        if (await _context.Slots.AnyAsync(ct))
            return;

        // מחושב ביחס לתאריך ההרצה, כך שתמיד יש משבצות עתידיות. השעות ב-UTC (06:00 UTC ≈ 08:00/09:00 בישראל)
        var today = DateTime.UtcNow.Date;
        var slots = new List<Slot>();

        for (var day = 1; day <= SeedDays; day++)
        {
            var date = DateTime.SpecifyKind(today.AddDays(day), DateTimeKind.Utc);
            if (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
                continue;

            for (var i = 0; i < 6; i++)
            {
                slots.Add(new Slot
                {
                    StartTimeUtc = date.AddHours(6).AddMinutes(30 * i),
                    NurseName = Nurses[i % Nurses.Length],
                    Status = SlotStatus.Available,
                    // כל משבצת פתוחה לשני סוגי ביקור, בסבב
                    VisitTypes = { visitTypes[i % visitTypes.Count], visitTypes[(i + 1) % visitTypes.Count] }
                });
            }
        }

        _context.Slots.AddRange(slots);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} slots", slots.Count);
    }
}
