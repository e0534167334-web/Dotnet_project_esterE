using Core.Common;
using Core.DTOs;

namespace Core.Interfaces.Services;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}
