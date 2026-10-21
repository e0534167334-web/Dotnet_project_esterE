using Baby_Clinic.Service.Services;
using Core.Common;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Baby_Clinic.Tests.Services;

// =====================================================================
// שלב 8 – Unit Tests לשירות התורים (חובה)
// ---------------------------------------------------------------------
// השירות נבדק בבידוד: כל ה-repositories מדומים עם Moq, אין בסיס נתונים.
// שמות הבדיקות: Method_Scenario_ExpectedResult. מבנה: Arrange / Act / Assert.
// =====================================================================
public class AppointmentServiceTests
{
    private const int ParentId = 1;
    private const int OtherParentId = 2;
    private const int ChildId = 5;
    private const int SlotId = 10;

    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<ISlotRepository> _slots = new();
    private readonly Mock<IChildRepository> _children = new();
    private readonly Mock<ILogger<AppointmentService>> _logger = new();
    private readonly AppointmentService _service;

    // ברירת המחדל של כל בדיקה: משבצת עתידית פנויה, ילד של ההורה המחובר, אין לו תורים פעילים
    private readonly Slot _slot = new()
    {
        Id = SlotId,
        StartTimeUtc = DateTime.UtcNow.AddDays(1),
        NurseName = "רחל כהן",
        Status = SlotStatus.Available
    };

    private readonly Child _child = new() { Id = ChildId, Name = "נועה", UserId = ParentId };

    private static readonly BookAppointmentRequest Request = new() { ChildId = ChildId, SlotId = SlotId };

    public AppointmentServiceTests()
    {
        _slots.Setup(r => r.GetByIdAsync(SlotId, It.IsAny<CancellationToken>())).ReturnsAsync(_slot);
        _children.Setup(r => r.GetByIdAsync(ChildId, It.IsAny<CancellationToken>())).ReturnsAsync(_child);
        _appointments.Setup(r => r.CountActiveByChildAsync(ChildId, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _service = new AppointmentService(_appointments.Object, _slots.Object, _children.Object,
            TestMapper.Create(), _logger.Object);
    }

    // ---------- קביעת תור ----------

    [Fact]
    public async Task BookAsync_AvailableSlot_ReturnsSuccessAndBooksSlot()
    {
        // Act
        var result = await _service.BookAsync(ParentId, Request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SlotStatus.Booked, _slot.Status);
        Assert.Equal(SlotId, result.Value!.SlotId);
        Assert.Equal("נועה", result.Value.ChildName);
        _appointments.Verify(r => r.AddAsync(It.Is<Appointment>(a => a.SlotId == SlotId && a.ChildId == ChildId),
            It.IsAny<CancellationToken>()), Times.Once);
        _appointments.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookAsync_BookedSlot_ReturnsConflictAndDoesNotSave()
    {
        // Arrange
        _slot.Status = SlotStatus.Booked;

        // Act
        var result = await _service.BookAsync(ParentId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        _appointments.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // שלב 7 + 8: המשאב המוגבל - הורה אחר שמר את המשבצת בין הטעינה לשמירה
    [Fact]
    public async Task BookAsync_RepositoryThrowsConcurrencyConflict_ReturnsConflictAndLogsWarning()
    {
        // Arrange
        _appointments.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("modified"));

        // Act
        var result = await _service.BookAsync(ParentId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("another parent", result.Message);
        VerifyWarningLogged(Times.Once());
    }

    [Fact]
    public async Task BookAsync_SlotDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var request = new BookAppointmentRequest { ChildId = ChildId, SlotId = 999 };

        // Act
        var result = await _service.BookAsync(ParentId, request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        _appointments.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BookAsync_SlotInThePast_ReturnsValidation()
    {
        // Arrange
        _slot.StartTimeUtc = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = await _service.BookAsync(ParentId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Validation, result.Status);
        _appointments.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BookAsync_ChildReachedActiveAppointmentsLimit_ReturnsValidation()
    {
        // Arrange
        _appointments.Setup(r => r.CountActiveByChildAsync(ChildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppointmentService.MaxActiveAppointmentsPerChild);

        // Act
        var result = await _service.BookAsync(ParentId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Validation, result.Status);
        Assert.Equal(SlotStatus.Available, _slot.Status);
        _appointments.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BookAsync_ChildOfAnotherParent_ReturnsForbidden()
    {
        // Act
        var result = await _service.BookAsync(OtherParentId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _appointments.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- ביטול תור ----------

    [Fact]
    public async Task CancelAsync_AppointmentOfAnotherParent_ReturnsForbidden()
    {
        // Arrange: תור פעיל של הילד של ParentId
        _slot.Status = SlotStatus.Booked;
        var appointment = new Appointment
        {
            Id = 7, ChildId = ChildId, Child = _child, SlotId = SlotId, Slot = _slot, Status = AppointmentStatus.Active
        };
        _appointments.Setup(r => r.GetWithDetailsAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);

        // Act: הורה אחר מנסה לבטל
        var result = await _service.CancelAsync(7, OtherParentId, Role.Parent, CancellationToken.None);

        // Assert: התור והמשבצת לא השתנו
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(AppointmentStatus.Active, appointment.Status);
        Assert.Equal(SlotStatus.Booked, _slot.Status);
        _appointments.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyWarningLogged(Times times) =>
        _logger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
}
