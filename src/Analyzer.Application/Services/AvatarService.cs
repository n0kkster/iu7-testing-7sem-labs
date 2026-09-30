using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Providers;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO.Common;

namespace Analyzer.Application.Services;

public class AvatarService(
    IAvatarRepository avatarRepository,
    IImageProvider imageProvider) : IAvatarService
{
    private readonly IAvatarRepository _avatarRepository = avatarRepository;
    private readonly IImageProvider _imageProvider = imageProvider;

    public async Task<Guid> UploadNewAvatarAsync(Guid userId, Stream fileStream)
    {
        var result = await _imageProvider.CreateWebpAsync(fileStream);

        var avatar = new Avatar(userId, result.Data, result.ContentType);

        var existingAvatar = await _avatarRepository.GetByHashAsync(userId, avatar.Hash);
        if (existingAvatar is not null)
            return existingAvatar.Id;
        
        await _avatarRepository.AddAsync(avatar);

        return avatar.Id;
    }

    public async Task<IReadOnlyCollection<AvatarDto>> GetAvatarHistoryAsync(Guid userId)
    {
        return await _avatarRepository.GetHistoryByUserIdAsync(userId);
    }

    public async Task<Avatar?> GetAvatarFileAsync(Guid avatarId)
    {
        return await _avatarRepository.GetByIdAsync(avatarId);
    }

    public async Task DeleteAvatarAsync(Guid userId, Guid avatarId)
    {
        var avatar = await _avatarRepository.GetByIdAsync(avatarId);
        
        if (avatar is null) 
            return;

        if (avatar.UserId != userId)
            throw new UnauthorizedAccessException("Удалять можно только свои аватары.");

        await _avatarRepository.DeleteAsync(avatarId);
    }
}