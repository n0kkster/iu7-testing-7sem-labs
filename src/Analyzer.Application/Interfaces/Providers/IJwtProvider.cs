using Analyzer.Domain.Entities;

namespace Analyzer.Application.Interfaces.Providers;

public interface IJwtProvider
{
    string GenerateToken(User user, string role);
}