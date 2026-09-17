using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;

using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;

namespace Analyzer.Client.Components.Pages.Admin;

public partial class AdminUsers : ComponentBase
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<UserDto> _allUsers = [];
    private List<UserDto> _filteredUsers = [];
    
    private List<TeamDto> _teams = [];
    private bool _isLoading = true;
    
    private Role? _selectedRoleFilter = null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;
        try 
        {
            _allUsers = await Http.GetFromJsonAsync<List<UserDto>>("api/v1/users") ?? [];
            _teams = await Http.GetFromJsonAsync<List<TeamDto>>("api/v1/teams") ?? [];
            
            _filteredUsers = _allUsers;
        }
        finally 
        { 
            _isLoading = false; 
            StateHasChanged(); 
        }
    }

    // ==========================================
    // ЛОГИКА ФИЛЬТРАЦИИ
    // ==========================================
    private void FilterByRole(Role? role)
    {
        _selectedRoleFilter = role;
        
        if (role.HasValue)
            _filteredUsers = _allUsers.Where(u => u.Role == role.Value).ToList();
        else
            _filteredUsers = _allUsers;
    }

    // ==========================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ UI
    // ==========================================
    private Color GetRoleColor(Role role) => role switch
    {
        Role.Admin => Color.Error,
        Role.Architect => Color.Secondary,
        Role.SRE => Color.Warning,
        _ => Color.Info
    };

    private string GetTeamName(Guid teamId)
    {
        var team = _teams.FirstOrDefault(t => t.Id == teamId);
        return team?.Name ?? "Неизвестная команда";
    }

    // ==========================================
    // УДАЛЕНИЕ
    // ==========================================

    private async Task DeleteUserAsync(UserDto user)
    {
        bool? result = await DialogService.ShowMessageBox(
            "Удаление пользователя",
            $"Вы уверены, что хотите навсегда удалить пользователя {user.Username}?",
            yesText: "Удалить", cancelText: "Отмена");

        if (result == true)
        {
            var response = await Http.DeleteAsync($"api/v1/users/{user.Id}");
            
            if (response.IsSuccessStatusCode)
            {
                _allUsers.Remove(user);
                FilterByRole(_selectedRoleFilter);
                Snackbar.Add("Пользователь успешно удален", Severity.Success);
            }
        }
    }
}