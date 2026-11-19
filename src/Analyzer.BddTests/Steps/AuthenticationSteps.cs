namespace Analyzer.BddTests.Steps;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Analyzer.BddTests.Hooks;
using Analyzer.BddTests.Support;
using Analyzer.Shared.DTO.Common;
using Analyzer.Shared.DTO.V2;
using FluentAssertions;
using Reqnroll;

[Binding]
public class AuthenticationSteps(ScenarioDataContext ctx)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Given("технический пользователь с включенной 2FA подготовлен в системе")]
    public void GivenTechUserReady()
    {
        ctx.TechUserId.Should().NotBeEmpty();
        ctx.TechUsername.Should().NotBeNullOrWhiteSpace();
    }

    [Given("учетная запись пользователя заблокирована после (\\d+) неверных попыток")]
    public async Task GivenAccountIsLocked(int attempts)
    {
        await WhenUserEntersWrongPasswordNTimes(attempts);
        ctx.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Locked);
    }

    [Given("пользователь успешно вошел в систему через 2FA")]
    public async Task GivenUserLoggedInWith2Fa()
    {
        await WhenFull2FaLoginWithPassword(SecretProvider.TechUserPassword);
        ctx.UserJwtToken.Should().NotBeNullOrWhiteSpace();
    }

    [When("пользователь вводит корректные логин и пароль")]
    public async Task WhenUserEntersValidCredentials()
    {
        await EnvClearMailAndLoginAsync(SecretProvider.TechUserPassword);
    }

    [When("пользователь пытается войти с корректным паролем")]
    public async Task WhenUserTriesValidLogin()
    {
        ctx.LastResponse = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = ctx.TechUsername,
            Password = SecretProvider.TechUserPassword
        });
    }

    [When("пользователь пытается войти со старым паролем")]
    public async Task WhenUserTriesOldPassword()
    {
        ctx.LastResponse = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = ctx.TechUsername,
            Password = SecretProvider.TechUserPassword
        });
    }

    [When("пользователь вводит неверный пароль (\\d+) раза подряд")]
    public async Task WhenUserEntersWrongPasswordNTimes(int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            ctx.LastResponse = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/login", new LoginDto
            {
                Username = ctx.TechUsername,
                Password = $"WrongPassword_{i}!"
            });
        }
    }

    [Then("система запрашивает подтверждение 2FA без выдачи токена")]
    public async Task ThenSystemRequires2FaWithoutToken()
    {
        ctx.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await ctx.LastResponse.Content.ReadFromJsonAsync<LoginResultDto>(JsonOptions);
        loginResult.Should().NotBeNull();
        loginResult!.RequiresTwoFactor.Should().BeTrue();
        loginResult.Token.Should().BeNull();
    }

    [Then("на почту пользователя приходит письмо с 6-значным кодом подтверждения")]
    public async Task ThenEmailContains2FaCode()
    {
        ctx.LastTwoFactorCode = await BddHooks.Env.Mailpit.WaitForSixDigitCodeAsync(ctx.TechEmail, "2FA");
        ctx.LastTwoFactorCode.Should().MatchRegex(@"^\d{6}$");
    }

    [When("пользователь отправляет полученный код 2FA")]
    public async Task WhenUserSubmits2FaCode()
    {
        ctx.LastResponse = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/verify-2fa",
            new VerifyTwoFactorDto(ctx.TechUsername, ctx.LastTwoFactorCode!));

        if (ctx.LastResponse.IsSuccessStatusCode)
        {
            var json = await ctx.LastResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            ctx.UserJwtToken = json.GetProperty("token").GetString();
        }
    }

    [Then("система выдает валидный JWT-токен доступа")]
    public void ThenSystemReturnsValidJwt()
    {
        ctx.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        ctx.UserJwtToken.Should().NotBeNullOrWhiteSpace();
        ctx.UserJwtToken!.Split('.').Should().HaveCount(3);
    }

    [Then("пользователь успешно получает свой профиль по этому токену")]
    public async Task ThenUserCanFetchProfile()
    {
        ctx.ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.UserJwtToken);
        var profileRes = await ctx.ApiClient.GetAsync($"/api/v2/users/{ctx.TechUserId}");
        profileRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await profileRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        profile.GetProperty("username").GetString().Should().Be(ctx.TechUsername);
    }

    [Then("учетная запись блокируется со статусом 423 Locked")]
    [Then("вход отклоняется со статусом 423 Locked")]
    public void ThenStatusIsLocked()
    {
        ctx.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Locked);
    }

    [Then("вход отклоняется со статусом 400 BadRequest")]
    public void ThenStatusIsBadRequest()
    {
        ctx.LastResponse!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [When("пользователь запрашивает код разблокировки на почту")]
    public async Task WhenUserRequestsUnlock()
    {
        await BddHooks.Env.Mailpit.ClearMessagesAsync();
        ctx.LastResponse = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/request-unlock",
            new RequestUnlockDto(ctx.TechUsername));
        ctx.LastResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then("на почту пользователя приходит письмо с кодом разблокировки")]
    public async Task ThenEmailContainsUnlockCode()
    {
        ctx.LastUnlockCode = await BddHooks.Env.Mailpit.WaitForSixDigitCodeAsync(ctx.TechEmail, "Восстановление");
        ctx.LastUnlockCode.Should().MatchRegex(@"^\d{6}$");
    }

    [When("пользователь подтверждает разблокировку полученным кодом")]
    public async Task WhenUserConfirmsUnlock()
    {
        ctx.LastResponse = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/confirm-unlock",
            new ConfirmUnlockDto(ctx.TechUsername, ctx.LastUnlockCode!));
    }

    [Then("аккаунт успешно разблокируется")]
    public void ThenAccountIsUnlocked()
    {
        ctx.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [When("пользователь выполняет плановую смену пароля на новый секретный пароль")]
    public async Task WhenUserPerformsPlannedPasswordChange()
    {
        ctx.ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.UserJwtToken);
        ctx.LastResponse = await ctx.ApiClient.PatchAsJsonAsync($"/api/v2/users/{ctx.TechUserId}", new
        {
            oldPassword = SecretProvider.TechUserPassword,
            newPassword = SecretProvider.TechUserNewPassword
        });
    }

    [Then("смена пароля завершается успешно")]
    public void ThenPasswordChangeSucceeds()
    {
        ctx.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [When("пользователь проходит полную 2FA-аутентификацию с корректным паролем")]
    public async Task WhenFull2FaLoginWithOldPassword() =>
        await WhenFull2FaLoginWithPassword(SecretProvider.TechUserPassword);

    [When("пользователь проходит полную 2FA-аутентификацию с новым паролем")]
    public async Task WhenFull2FaLoginWithNewPassword() =>
        await WhenFull2FaLoginWithPassword(SecretProvider.TechUserNewPassword);

    private async Task WhenFull2FaLoginWithPassword(string password)
    {
        await EnvClearMailAndLoginAsync(password);
        await ThenSystemRequires2FaWithoutToken();
        await ThenEmailContains2FaCode();
        await WhenUserSubmits2FaCode();
    }

    private async Task EnvClearMailAndLoginAsync(string password)
    {
        await BddHooks.Env.Mailpit.ClearMessagesAsync();
        ctx.ApiClient.DefaultRequestHeaders.Authorization = null;
        ctx.LastResponse = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = ctx.TechUsername,
            Password = password
        });
    }
}