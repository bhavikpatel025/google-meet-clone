using VideoCallApp.Domain.Entities;

namespace VideoCallApp.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}