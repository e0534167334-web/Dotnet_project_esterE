using System.Security.Claims;
using System.Text;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Services;
using Core.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Baby_Clinic.Service.Security;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public TokenResponse Generate(User user)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key))
            throw new InvalidOperationException("JWT signing key is not configured (Jwt:Key in User Secrets).");

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        return new TokenResponse
        {
            Token = new JsonWebTokenHandler().CreateToken(descriptor),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
