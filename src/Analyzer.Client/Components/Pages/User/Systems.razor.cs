namespace Analyzer.Client.Components.Pages.User;

using Analyzer.Shared.DTO.Common;
using Analyzer.Client.Components.Dialogs;

using MudBlazor;
using Serilog;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;

public partial class Systems : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState> authStateTask { get; set; } = default!;

    private bool _isLoading = true;
    private string _searchString = "";
    private List<ITSystemDto> _systems = [];

    private UserDto _loggedUser = default!;

    private bool _isImportingSystem = false;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    protected override async Task OnInitializedAsync()
    {
        await LoadSystemsAsync();
    }

    private async Task LoadSystemsAsync()
    {
        _isLoading = true;
        try
        {
            var authState = await authStateTask;
            var userIdStr = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdStr, out var userId))
            {
                _loggedUser = await Http.GetFromJsonAsync<UserDto>($"api/v2/users/{userId}")
                    ?? throw new KeyNotFoundException("Профиль пользователя не найден.");

                _systems = await Http.GetFromJsonAsync<List<ITSystemDto>>(
                    $"api/v2/systems?teamId={_loggedUser.TeamId}") ?? [];
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка загрузки систем");
            Snackbar.Add("Ошибка загрузки систем", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OpenAddSystemDialogAsync()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var parameters = new DialogParameters
        {
            ["TeamId"] = _loggedUser.TeamId,
        };

        var dialog = await DialogService.ShowAsync<CreateSystemDialog>("Новая система", parameters, options);
        var result = await dialog.Result;
        if ((!result?.Canceled) ?? false)
            await LoadSystemsAsync();
    }

    private async Task DeleteSystemAsync(ITSystemDto system)
    {
        bool? result = await DialogService.ShowMessageBox(
            "Подтверждение удаления", 
            $"Вы уверены, что хотите удалить систему '{system.Name}'? Это действие необратимо.", 
            yesText: "Удалить", cancelText: "Отмена");

        if (result == true)
        {
            try
            {
                await Http.DeleteAsync($"api/v2/systems/{system.Id}");
                _systems.Remove(system);
                Snackbar.Add($"Система {system.Name} удалена", Severity.Success);
            }
            catch (Exception)
            {
                Snackbar.Add("Ошибка при удалении", Severity.Error);
            }
        }
    }

    private async Task ImportSystemAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null) 
            return;

        var systemDto = await GetImportDtoFromDialogAsync();
        if (systemDto is null) 
            return; 

        _isImportingSystem = true;
        StateHasChanged();

        try
        {
            await SendImportRequestAsync(file, systemDto);

            Snackbar.Add("Система успешно импортирована", Severity.Success);
            await LoadSystemsAsync();
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP ошибка при импорте системы. Код: {StatusCode}", ex.StatusCode);
            Snackbar.Add("Ошибка сервера при импорте системы", Severity.Error);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Непредвиденная ошибка импорта: {Message}", ex.Message);
            Snackbar.Add("Не удалось импортировать систему", Severity.Error);
        }
        finally
        {
            _isImportingSystem = false;
            StateHasChanged();
        }
    }

    private async Task<CreateITSystemDto?> GetImportDtoFromDialogAsync()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var parameters = new DialogParameters
        {
            ["TeamId"] = _loggedUser.TeamId,
        };

        var dialog = await DialogService.ShowAsync<ImportSystemDialog>("Импорт системы", parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled || result.Data is not CreateITSystemDto dto)
            return null;

        return dto;
    }

    private async Task SendImportRequestAsync(IBrowserFile file, CreateITSystemDto dto)
    {
        using var content = new MultipartFormDataContent();

        using var stream = file.OpenReadStream(MaxFileSize);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", file.Name);

        var jsonDto = JsonSerializer.Serialize(dto);
        var dtoContent = new StringContent(jsonDto, System.Text.Encoding.UTF8, "application/json");
        content.Add(dtoContent, "importData");

        var response = await Http.PostAsync("api/v2/systems/import", content);
        
        response.EnsureSuccessStatusCode(); 
    }

    private async Task ExportSystemAsync(Guid id)
    {
        var authState = await authStateTask;
        var user = authState.User;
        var token = user.FindFirst("jwt-api-token")?.Value;

        NavManager.NavigateTo(
            $"http://localhost:1555/api/v2/systems/{id}/export?access_token={token}", 
            forceLoad: true);
    }
}