using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO.Common;

namespace Analyzer.Application.Interfaces.Services;

public interface IAvatarService
{
    public Task<Guid> UploadNewAvatarAsync(Guid userId, Stream fileStream);
    public Task<IReadOnlyCollection<AvatarDto>> GetAvatarHistoryAsync(Guid userId);
    public Task<Avatar?> GetAvatarFileAsync(Guid avatarId);
    public Task DeleteAvatarAsync(Guid userId, Guid avatarId);
}

