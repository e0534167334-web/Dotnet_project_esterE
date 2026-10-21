using Baby_Clinic.Service.Services;
using Core.Common;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Moq;

namespace Baby_Clinic.Tests.Services;

// =====================================================================
// שלב 8 – Unit Tests לשירות המשבצות
// =====================================================================
public class SlotServiceTests
{
    private readonly Mock<ISlotRepository> _slots = new();
    private readonly Mock<IVisitTypeRepository> _visitTypes = new();
    private readonly SlotService _service;

    public SlotServiceTests()
    {
        _service = new SlotService(_slots.Object, _visitTypes.Object, TestMapper.Create());
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessAndSavesOnce()
    {
        // Arrange
        var vaccine = new VisitType { Id = 1, Name = "חיסון" };
        _visitTypes.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VisitType> { vaccine });

        var request = new CreateSlotRequest
        {
            StartTimeUtc = DateTime.UtcNow.AddDays(2),
            NurseName = "מרים לוי",
            VisitTypeIds = new List<int> { 1 }
        };

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("מרים לוי", result.Value!.NurseName);
        Assert.Equal(SlotStatus.Available, result.Value.Status);
        Assert.Single(result.Value.VisitTypes);
        _slots.Verify(r => r.AddAsync(It.IsAny<Slot>(), It.IsAny<CancellationToken>()), Times.Once);
        _slots.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SlotWithAppointments_ReturnsConflictAndDoesNotRemove()
    {
        // Arrange: משבצת תפוסה (יש לה תור)
        var slot = new Slot { Id = 3, Status = SlotStatus.Booked, StartTimeUtc = DateTime.UtcNow.AddDays(1) };
        _slots.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _slots.Setup(r => r.HasAppointmentsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(3, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        _slots.Verify(r => r.Remove(It.IsAny<Slot>()), Times.Never);
        _slots.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_PageAndPageSize_ArePassedToRepository()
    {
        // Arrange
        var search = new SlotSearchParams { Page = 3, PageSize = 5, AvailableOnly = true };
        _slots.Setup(r => r.SearchAsync(It.IsAny<SlotSearchParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Slot> { new() { Id = 11, NurseName = "רחל כהן" } }, 42));

        // Act
        var result = await _service.SearchAsync(search, CancellationToken.None);

        // Assert: ה-pagination נעשה ב-repository (בשאילתה), לא בזיכרון של השירות
        _slots.Verify(r => r.SearchAsync(
            It.Is<SlotSearchParams>(p => p.Page == 3 && p.PageSize == 5 && p.AvailableOnly),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Page);
        Assert.Equal(5, result.Value.PageSize);
        Assert.Equal(42, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
    }
}
