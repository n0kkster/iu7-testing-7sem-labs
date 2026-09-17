using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using MudBlazor;
using Serilog;

using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;
using Analyzer.Client.Components.Dialogs;

namespace Analyzer.Client.Components.Pages.Admin;

public partial class AdminTeams : ComponentBase
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    public class TeamViewModel : TeamDto
    {
        public bool ShowDetails { get; set; }
        public int MembersCount { get; set; } = 0;
        public List<InviteDto> ActiveInvites { get; set; } = [];
    }

    private List<TeamViewModel> _teams = [];
    private bool _isLoading = true;

    private string _icon = Icons.Material.Filled.ContentCopy;
    private Color _iconColor = Color.Default;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;
        try
        {
            var teams = await Http.GetFromJsonAsync<List<TeamDto>>("api/v1/teams") ?? [];
            
            _teams = teams.Select(team => new TeamViewModel 
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                MembersCount = team.Members?.Count ?? 0
            }).ToList();

            foreach (var team in _teams)
            {
                try 
                {
                    var invites = await Http.GetFromJsonAsync<List<InviteDto>>($"api/v1/invites/{team.Id}") ?? [];

                    team.ActiveInvites = invites
                        .Where(i => i.Status == InviteStatus.Pending)
                        .ToList();
                }
                catch 
                { 
                    team.ActiveInvites = []; 
                    break; 
                }
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged(); 
        }
    }

    // ====================================================================
    // ИНВАЙТЫ
    // ====================================================================

    private async Task GenerateInvite(Guid teamId)
    {

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };
    
        var parameters = new DialogParameters
        {
            ["TeamId"] = teamId,
        };

        var dialog = await DialogService.ShowAsync<CreateInviteDialog>("Новое приглашение", parameters, options);
        var result = await dialog.Result;

        if ((!result?.Canceled ?? false) && result!.Data is InviteDto newInvite)
        {
            var team = _teams.First(t => t.Id == teamId);
            team.ActiveInvites.Add(newInvite);
            Snackbar.Add("Приглашение успешно сгенерировано", Severity.Success);
        }
    }

    private async Task RevokeInvite(TeamViewModel team, Guid inviteId)
    {
        var response = await Http.DeleteAsync($"api/v1/invites/{inviteId}");
        response.EnsureSuccessStatusCode();

        team.ActiveInvites.RemoveAll(i => i.Id == inviteId);
        Snackbar.Add("Приглашение успешно отозвано", Severity.Info);
    }

    // ====================================================================
    // УПРАВЛЕНИЕ КОМАНДАМИ
    // ====================================================================

    private async Task OpenCreateTeamDialog()
    {
        var options = new DialogOptions 
        { 
            CloseOnEscapeKey = true, 
            MaxWidth = MaxWidth.Small, 
            FullWidth = true 
        };
        
        var dialog = await DialogService.ShowAsync<CreateTeamDialog>("Создать команду", options);
        var result = await dialog.Result;

        if (result is null)
            return;

        if (!result.Canceled && result.Data is TeamDto newTeam)
        {
            _teams.Add(new TeamViewModel 
            {
                Id = newTeam.Id,
                Name = newTeam.Name,
                Description = newTeam.Description,
                MembersCount = 0,
                ActiveInvites = []
            });
            
            Snackbar.Add($"Команда {newTeam.Name} успешно создана!", Severity.Success);
        }
    }

    private async Task DeleteTeam(TeamViewModel team)
    {
        bool? confirm = await DialogService.ShowMessageBox(
            "Удаление команды",
            $"Вы уверены, что хотите удалить команду '{team.Name}'?",
            yesText: "Удалить", cancelText: "Отмена");

        if (confirm == true)
        {
            var response = await Http.DeleteAsync($"api/v1/teams/{team.Id}");
            response.EnsureSuccessStatusCode();

            _teams.Remove(team);
            Snackbar.Add("Команда удалена", Severity.Success);
        }
    }

    private async Task CopyAsync(string text)
    {
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        
        _icon = Icons.Material.Filled.Check;
        _iconColor = Color.Success;
        StateHasChanged();

        await Task.Delay(2000);
        
        _icon = Icons.Material.Filled.ContentCopy;
        _iconColor = Color.Default;
    }
}