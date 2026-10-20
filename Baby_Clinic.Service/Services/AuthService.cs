using Core.Common;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Baby_Clinic.Service.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(IUserRepository users, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        // PostgreSQL רגיש לאותיות, ולכן המייל מנורמל לפני הבדיקה והשמירה
        var email = NormalizeEmail(request.Email);

        if (await _users.GetByEmailAsync(email, ct) is not null)
            return Result.Fail(ResultStatus.Conflict, "A user with this email already exists.");

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = Role.Parent // הרשמה תמיד יוצרת הורה
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        // אותה הודעה למייל לא קיים ולסיסמה שגויה, כדי לא לחשוף אילו מיילים רשומים
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<TokenResponse>.Fail(ResultStatus.Unauthorized, "Invalid email or password.");

        return Result<TokenResponse>.Success(_tokenGenerator.Generate(user));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
