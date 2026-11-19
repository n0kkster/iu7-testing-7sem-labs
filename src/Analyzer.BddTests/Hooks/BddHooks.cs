namespace Analyzer.BddTests.Hooks;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Analyzer.BddTests.Support;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO.Common;
using Analyzer.Shared.DTO.V2;
using Reqnroll;

[Binding]
public class BddHooks(ScenarioDataContext ctx)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public static BddTestEnvironment Env { get; private set; } = null!;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        Env = new BddTestEnvironment();
        await Env.InitializeAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        await Env.DisposeAsync();
    }

    [BeforeScenario]
    public async Task CreateTechnicalUserAsync()
    {
        ctx.ApiClient = Env.CreateApiClient();
        await Env.Mailpit.ClearMessagesAsync();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        ctx.TechUsername = $"bdd_tech_{suffix}";
        ctx.TechEmail = $"bdd_tech_{suffix}@analyzer.local";

        // 1. Логин администратора с паролем из секретов
        var adminLoginRes = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = SecretProvider.AdminUsername,
            Password = SecretProvider.AdminPassword
        });
        adminLoginRes.EnsureSuccessStatusCode();

        var adminJson = await adminLoginRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        ctx.AdminToken = adminJson.GetProperty("token").GetString()!;
        ctx.ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AdminToken);

        // 2. Создаем команду и инвайт для технического пользователя
        var teamRes = await ctx.ApiClient.PostAsJsonAsync("/api/v2/teams", new CreateTeamDto
        {
            Name = $"BDD_Team_{suffix}",
            Description = "Temporary BDD technical team"
        });
        teamRes.EnsureSuccessStatusCode();
        ctx.TeamId = (await teamRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        var inviteRes = await ctx.ApiClient.PostAsJsonAsync($"/api/v2/teams/{ctx.TeamId}/invites", new
        {
            email = ctx.TechEmail,
            validForDays = 1,
            role = Role.Developer
        });
        inviteRes.EnsureSuccessStatusCode();
        var inviteCode = (await inviteRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("code").GetString()!;

        // 3. Регистрируем технического пользователя с паролем из секретов
        ctx.ApiClient.DefaultRequestHeaders.Authorization = null;
        var regRes = await ctx.ApiClient.PostAsJsonAsync("/api/v2/users/register", new RegisterDto
        {
            Username = ctx.TechUsername,
            Email = ctx.TechEmail,
            Password = SecretProvider.TechUserPassword,
            InviteCode = inviteCode
        });
        regRes.EnsureSuccessStatusCode();
        ctx.TechUserId = await regRes.Content.ReadFromJsonAsync<Guid>();

        // 4. Включаем ему 2FA через API администратора
        ctx.ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AdminToken);
        var enable2FaRes = await ctx.ApiClient.PutAsJsonAsync($"/api/v2/users/{ctx.TechUserId}/2fa", new ToggleTwoFactorDto(true));
        enable2FaRes.EnsureSuccessStatusCode();

        ctx.ApiClient.DefaultRequestHeaders.Authorization = null;
    }

    [AfterScenario]
    public async Task CleanupTechnicalUserAsync()
    {
        if (string.IsNullOrEmpty(ctx.AdminToken))
            return;

        ctx.ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AdminToken);

        if (ctx.TechUserId != Guid.Empty)
            await ctx.ApiClient.DeleteAsync($"/api/v2/users/{ctx.TechUserId}");

        if (ctx.TeamId != Guid.Empty)
            await ctx.ApiClient.DeleteAsync($"/api/v2/teams/{ctx.TeamId}");

        ctx.ApiClient.Dispose();
    }
}