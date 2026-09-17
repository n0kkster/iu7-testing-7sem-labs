using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

namespace Analyzer.Application.Services;

public class TeamService(
    ITeamRepository teamRepository, 
    ISystemRepository systemRepository,
    IUserRepository userRepository) : ITeamService
{
    private readonly ITeamRepository _teamRepository = teamRepository;
    private readonly ISystemRepository _systemRepository = systemRepository;
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<IReadOnlyCollection<TeamDto>> GetAllTeamsAsync()
    {
        var teams = await _teamRepository.GetAllTeamsAsync();
        return teams.Select(MapToDto).ToList();
    }

    public async Task AddMemberAsync(Guid teamId, User user)
    {
        var team = await _teamRepository.GetByIdAsync(teamId)
            ?? throw new KeyNotFoundException("Команда не найдена");

        team.AddMember(user.Id);
    }

    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto dto)
    {
        var team = new Team(dto.Name, dto.Description);
        await _teamRepository.AddAsync(team);
        return MapToDto(team);
    }

    public async Task DeleteTeamAsync(Guid teamId)
    {
        var systems = await _systemRepository.GetByTeamIdAsync(teamId);
        if (systems.Any())
            throw new InvalidOperationException("Невозможно удалить команду, владеющую хоть одной системой");

        await _teamRepository.DeleteAsync(teamId);
    }

    public async Task<bool> ExistsAsync(Guid teamId)
    {
        return (await _teamRepository.GetByIdAsync(teamId)) is not null;
    }

    public async Task<IReadOnlyCollection<UserDto>> GetTeamMembersAsync(Guid teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId)
            ?? throw new KeyNotFoundException("Команда не найдена");
        
        List<UserDto> userDtos = [];
        foreach (var id in team.MemberIds)
        {
            var user = await _userRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Пользователь с id {id} не существует");

            var dto = MapToDto(user);
            userDtos.Add(dto);
        }

        return userDtos;
    }

    public async Task RemoveMemberAsync(Guid teamId, Guid targetUserId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId)
            ?? throw new KeyNotFoundException("Команда не найдена");

        team.RemoveMember(targetUserId);
    }

    public async Task UpdateTeamAsync(Guid teamId, CreateTeamDto dto)
    {
        var team = await _teamRepository.GetByIdAsync(teamId)
            ?? throw new KeyNotFoundException("Команда не найдена");

        team.UpdateProfile(dto.Name, dto.Description);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            TeamId = user.TeamId ?? Guid.Empty
        };
    }

    private TeamDto MapToDto(Team team)
    {
        return new TeamDto
        {
            Id = team.Id, 
            Name = team.Name,
            Description = team.Description,
            Members = team.MemberIds
        };
    }
}