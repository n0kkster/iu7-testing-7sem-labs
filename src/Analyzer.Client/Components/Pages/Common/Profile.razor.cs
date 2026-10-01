namespace Analyzer.Client.Components.Pages.Common;

using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Analyzer.Shared.DTO.Common;
using Analyzer.Client.Components.Dialogs;

public partial class Profile : ComponentBase
{
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private bool _isLoading = true;
    private UserDto? _user;
    private Guid _currentUserId;

    private bool _isSavingProfile = false;
    private bool _isSavingPassword = false;

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
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userIdStr = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdStr, out var userId))
            {
                _currentUserId = userId;
                _user = await Http.GetFromJsonAsync<UserDto>($"api/v2/users/{_currentUserId}");

                if (_user is not null)
                {
                    _profileModel.Username = _user.Username;
                    _profileModel.Email = _user.Email;
                    _profileModel.AvatarId = _user.AvatarId;
                }

                await LoadAvatarImage(_profileModel.AvatarId);
            }
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
            var patchDto = new PatchUserDto(
                Username: _profileModel.Username,
                Email: _profileModel.Email,
                AvatarId: _profileModel.AvatarId
            );

            var response = await Http.PatchAsJsonAsync($"api/v2/users/{_currentUserId}", patchDto);
            
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
            var patchDto = new PatchUserDto(
                OldPassword: _passwordModel.OldPassword,
                NewPassword: _passwordModel.NewPassword
            );

            var response = await Http.PatchAsJsonAsync($"api/v2/users/{_currentUserId}", patchDto);

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
        _showOldPassword = !_showOldPassword;
        _oldPasswordIcon = _showOldPassword ? Icons.Material.Filled.Visibility : Icons.Material.Filled.VisibilityOff;
        _oldPasswordInput = _showOldPassword ? InputType.Text : InputType.Password;
    }

    private void ToggleNewPasswordVisibility()
    {
        _showNewPassword = !_showNewPassword;
        _newPasswordIcon = _showNewPassword ? Icons.Material.Filled.Visibility : Icons.Material.Filled.VisibilityOff;
        _newPasswordInput = _showNewPassword ? InputType.Text : InputType.Password;
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
            var response = await Http.GetAsync($"/api/v2/avatars/{id}");

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/webp";
                _avatarImageString = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch { }
    }
}