using Microsoft.AspNetCore.Components;
using MudBlazor;

using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;
using Analyzer.Client.Components.Dialogs;

namespace Analyzer.Client.Components.Pages.Common;

public partial class Profile : ComponentBase
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private bool _isLoading = true;
    private UserDto? _user;

    private bool _isSavingProfile = false;
    private bool _isSavingPassword = false;

    // Модели форм
    private UpdateProfileViewModel _profileModel = new();
    private ChangePasswordViewModel _passwordModel = new();

    private bool _showOldPassword;
    private InputType _oldPasswordInput = InputType.Password;
    private string _oldPasswordIcon = Icons.Material.Filled.VisibilityOff;

    private bool _showNewPassword;
    private InputType _newPasswordInput = InputType.Password;
    private string _newPasswordIcon = Icons.Material.Filled.VisibilityOff;

    private string _avatarImageString = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _user = await Http.GetFromJsonAsync<UserDto>("api/v1/users/me");
            if (_user is not null)
            {
                _profileModel.Username = _user.Username;
                _profileModel.Email = _user.Email;
                _profileModel.AvatarId = _user.AvatarId;
            }

            await LoadAvatarImage(_profileModel.AvatarId);
        }
        catch { }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task UpdateProfileAsync()
    {
        _isSavingProfile = true;
        try
        {
            var response = await Http.PutAsJsonAsync("api/v1/users/me/profile", _profileModel);
            
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Профиль успешно обновлен", Severity.Success);
                if (_user is not null)
                {
                    _user.Username = _profileModel.Username;
                    _user.Email = _profileModel.Email;
                }
            }
        }
        catch { }
        finally
        {
            _isSavingProfile = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        _isSavingPassword = true;
        try
        {
            var request = new {
                _passwordModel.OldPassword,
                _passwordModel.NewPassword 
            };

            var response = await Http.PutAsJsonAsync("api/v1/users/me/password", request);

            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Пароль успешно изменен", Severity.Success);
                _passwordModel = new ChangePasswordViewModel();
            }
        }
        catch { }
        finally
        {
            _isSavingPassword = false;
        }
    }

    private void ToggleOldPasswordVisibility()
    {
        if (_showOldPassword)
        {
            _showOldPassword = false;
            _oldPasswordIcon = Icons.Material.Filled.VisibilityOff;
            _oldPasswordInput = InputType.Password;
        }
        else
        {
            _showOldPassword = true;
            _oldPasswordIcon = Icons.Material.Filled.Visibility;
            _oldPasswordInput = InputType.Text;
        }
    }

    private void ToggleNewPasswordVisibility()
    {
        if (_showNewPassword)
        {
            _showNewPassword = false;
            _newPasswordIcon = Icons.Material.Filled.VisibilityOff;
            _newPasswordInput = InputType.Password;
        }
        else
        {
            _showNewPassword = true;
            _newPasswordIcon = Icons.Material.Filled.Visibility;
            _newPasswordInput = InputType.Text;
        }
    }
    private async Task OpenAvatarDialog()
    {
        var parameters = new DialogParameters<AvatarDialog>
        {
            { x => x.CurrentAvatarId, _profileModel.AvatarId } 
        };

        var options = new DialogOptions 
        { 
            CloseOnEscapeKey = true, 
            MaxWidth = MaxWidth.Small, 
            FullWidth = true 
        };
        
        var dialog = await DialogService.ShowAsync<AvatarDialog>("Изменить аватар", parameters, options);
        var result = await dialog.Result;

        if ((!result?.Canceled ?? false) && result!.Data is Guid selectedAvatarId)
        {
            _profileModel.AvatarId = selectedAvatarId;
            await LoadAvatarImage(_profileModel.AvatarId);
        }
    }

    private async Task LoadAvatarImage(Guid? id)
    {
        if (id is null)
            return;

        try
        {
            var response = await Http.GetAsync($"/api/v1/avatars/{id}");

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/webp";
                _avatarImageString = 
                    $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch { }
    }
}