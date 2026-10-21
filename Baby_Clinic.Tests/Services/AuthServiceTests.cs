using Baby_Clinic.Service.Services;
using Core.Common;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Moq;

namespace Baby_Clinic.Tests.Services;

// =====================================================================
// שלב 8 – Unit Tests לשירות ההרשמה וההתחברות
// =====================================================================
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenGenerator> _tokenGenerator = new();
    private readonly AuthService _service;

    private readonly User _existingUser = new()
    {
        Id = 1, Email = "parent@babyclinic.local", PasswordHash = "stored-hash", Role = Role.Parent
    };

    public AuthServiceTests()
    {
        _users.Setup(r => r.GetByEmailAsync(_existingUser.Email, It.IsAny<CancellationToken>())).ReturnsAsync(_existingUser);
        _service = new AuthService(_users.Object, _passwordHasher.Object, _tokenGenerator.Object);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsConflictAndDoesNotSave()
    {
        // Arrange: אותו מייל באותיות גדולות ורווחים - צריך להיחשב כפול אחרי נרמול
        var request = new RegisterRequest { Email = "  Parent@BabyClinic.local ", Password = "Secret123!" };

        // Act
        var result = await _service.RegisterAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorizedAndNoToken()
    {
        // Arrange
        _passwordHasher.Setup(h => h.Verify("wrong-password", "stored-hash")).Returns(false);
        var request = new LoginRequest { Email = _existingUser.Email, Password = "wrong-password" };

        // Act
        var result = await _service.LoginAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Null(result.Value);
        _tokenGenerator.Verify(g => g.Generate(It.IsAny<User>()), Times.Never);
    }
}
