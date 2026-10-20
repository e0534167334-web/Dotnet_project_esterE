using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Services;

public interface ITokenGenerator
{
    TokenResponse Generate(User user);
}
