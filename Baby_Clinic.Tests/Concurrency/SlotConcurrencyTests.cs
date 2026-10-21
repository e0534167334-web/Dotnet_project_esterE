using Baby_Clinic.Data.Repositories;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Baby_Clinic.Tests.Concurrency;

// =====================================================================
// שלב 7 – בדיקת ההוכחה: שני הורים, משבצת אחת
// ---------------------------------------------------------------------
// התרחיש: שני הורים טוענים את אותה משבצת פנויה באותו רגע, ושניהם מנסים
// לסמן אותה כתפוסה. בלי concurrency token שניהם היו מצליחים.
// עם Version (ממופה ל-xmin של PostgreSQL) - הראשון מצליח, השני נכשל.
// =====================================================================
public class SlotConcurrencyTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _database;

    public SlotConcurrencyTests(TestDatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task SaveChanges_TwoContextsBookSameSlot_SecondThrowsDbUpdateConcurrencyException()
    {
        // Arrange: משבצת פנויה אחת בבסיס הנתונים
        var slotId = await CreateAvailableSlotAsync();

        // שני מופעי AppDbContext נפרדים (= שתי בקשות של שני הורים) טוענים את אותה משבצת
        await using var parentA = _database.CreateContext();
        await using var parentB = _database.CreateContext();
        var slotOfA = await parentA.Slots.SingleAsync(s => s.Id == slotId);
        var slotOfB = await parentB.Slots.SingleAsync(s => s.Id == slotId);
        Assert.Equal(slotOfA.Version, slotOfB.Version); // שניהם ראו את אותה גרסה

        // Act: שניהם מסמנים את המשבצת כתפוסה
        slotOfA.Status = SlotStatus.Booked;
        slotOfB.Status = SlotStatus.Booked;

        await parentA.SaveChangesAsync(); // הראשון שומר בהצלחה - xmin של השורה משתנה

        // Assert: השני נשען על xmin ישן, ולכן ה-UPDATE לא נוגע באף שורה ו-EF זורק
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => parentB.SaveChangesAsync());

        // ובבסיס הנתונים נשארה רק השמירה של הראשון
        await using var verify = _database.CreateContext();
        var saved = await verify.Slots.AsNoTracking().SingleAsync(s => s.Id == slotId);
        Assert.Equal(SlotStatus.Booked, saved.Status);
        Assert.NotEqual(slotOfB.Version, saved.Version);
    }

    [Fact]
    public async Task SaveChangesAsync_ConcurrentUpdateThroughRepository_ThrowsConcurrencyConflictException()
    {
        // Arrange
        var slotId = await CreateAvailableSlotAsync();

        await using var contextA = _database.CreateContext();
        await using var contextB = _database.CreateContext();
        var repositoryA = new SlotRepository(contextA);
        var repositoryB = new SlotRepository(contextB);

        var slotOfA = (await repositoryA.GetByIdAsync(slotId, CancellationToken.None))!;
        var slotOfB = (await repositoryB.GetByIdAsync(slotId, CancellationToken.None))!;

        // Act
        slotOfA.Status = SlotStatus.Booked;
        slotOfB.Status = SlotStatus.Booked;
        await repositoryA.SaveChangesAsync(CancellationToken.None);

        // Assert: שכבת Data מתרגמת את החריגה של EF לחריגה שלנו מ-Core,
        // כך ששכבת Service לא צריכה להכיר את EF
        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => repositoryB.SaveChangesAsync(CancellationToken.None));
        Assert.IsType<DbUpdateConcurrencyException>(ex.InnerException);
    }

    private async Task<int> CreateAvailableSlotAsync()
    {
        await using var context = _database.CreateContext();
        var slot = new Slot
        {
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            NurseName = "אחות בדיקה",
            Status = SlotStatus.Available
        };
        context.Slots.Add(slot);
        await context.SaveChangesAsync();
        return slot.Id;
    }
}
