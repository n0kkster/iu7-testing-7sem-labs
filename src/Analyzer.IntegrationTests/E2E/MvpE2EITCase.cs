namespace Analyzer.IntegrationTests.E2E;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Shared.DTO.Common;
using Analyzer.Shared.DTO.V2;
using FluentAssertions;
using Xunit;

public class MvpScenarioE2EITCase : IClassFixture<FaultAnalyzerApiFactory>, IClassFixture<SharedNeo4jFixture>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SharedNeo4jFixture _neo4jFixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // ==========================================================
    // ТРЕКИНГ СУЩНОСТЕЙ ДЛЯ ПЕРСОНАЛЬНОЙ УБОРКИ МУСОРА
    // ==========================================================
    private readonly List<Guid> _trackedSystemIds = [];
    private readonly List<Guid> _trackedTeamIds = [];
    private readonly List<Guid> _trackedUserIds = [];
    private string? _adminTokenForTeardown;
    private string? _architectTokenForTeardown;

    public MvpScenarioE2EITCase(FaultAnalyzerApiFactory factory, SharedNeo4jFixture neo4jFixture)
    {
        _neo4jFixture = neo4jFixture;
        var standUrl = Environment.GetEnvironmentVariable("STAND_API_URL");
        if (!string.IsNullOrEmpty(standUrl))
        {
            _client = new HttpClient { BaseAddress = new Uri(standUrl) };
        }
        else
        {
            _client = factory.CreateClient();
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var sysId in _trackedSystemIds)
        {
            try { await _neo4jFixture.WipeSystemDataAsync(sysId); } 
            catch { }
        }

        if (!string.IsNullOrEmpty(_architectTokenForTeardown))
        {
            SetAuthorizationToken(_architectTokenForTeardown);
            foreach (var sysId in _trackedSystemIds)
            {
                await TryDeleteAsync($"/api/v2/systems/{sysId}");
            }
        }

        if (!string.IsNullOrEmpty(_adminTokenForTeardown))
        {
            SetAuthorizationToken(_adminTokenForTeardown);

            foreach (var userId in _trackedUserIds)
            {
                await TryDeleteAsync($"/api/v2/users/{userId}");
            }

            foreach (var teamId in _trackedTeamIds)
            {
                await TryDeleteAsync($"/api/v2/teams/{teamId}");
            }
        }
    }

    private async Task TryDeleteAsync(string url)
    {
        try
        {
            var response = await _client.DeleteAsync(url);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
                return;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"[Teardown Error] Не удалось выполнить DELETE {url}. Status: {response.StatusCode}. Response: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    [Fact]
    public async Task ExecuteFullMvpScenario_FromRegistrationToAnalysis_ViaHttpApi_V2()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
        var architectEmail = $"arch_{uniqueSuffix}@corp.local";
        var architectUser = $"architect_{uniqueSuffix}";
        var architectPass = "P@ssword1234!";

        // ==========================================================
        // ШАГ 1: Логин админа
        // ==========================================================
        var adminLoginResponse = await _client.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = "admin",
            Password = "AdminSecret123!"
        });
        
        var adminAuth = await adminLoginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        _adminTokenForTeardown = adminAuth.GetProperty("token").GetString();
        SetAuthorizationToken(_adminTokenForTeardown!);

        // ==========================================================
        // ШАГ 2: Создание команды
        // ==========================================================
        var createTeamResponse = await _client.PostAsJsonAsync("/api/v2/teams", new CreateTeamDto
        {
            Name = $"Platform_Core_{uniqueSuffix}",
            Description = "E2E Test Team"
        });
        var createdTeam = await createTeamResponse.Content.ReadFromJsonAsync<TeamDto>(JsonOptions);
        
        _trackedTeamIds.Add(createdTeam!.Id); // Трекаем команду!

        // ==========================================================
        // ШАГ 3: Выпуск инвайта
        // ==========================================================
        var inviteResponse = await _client.PostAsJsonAsync($"/api/v2/teams/{createdTeam.Id}/invites", new
        {
            email = architectEmail, validForDays = 7, role = Role.Architect
        });
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteDto>(JsonOptions);

        // ==========================================================
        // ШАГ 4: Регистрация Архитектора
        // ==========================================================
        ClearAuthorizationToken();
        var registerResponse = await _client.PostAsJsonAsync("/api/v2/users/register", new RegisterDto
        {
            Username = architectUser, Email = architectEmail, Password = architectPass, InviteCode = invite!.Code
        });
        var newUserId = await registerResponse.Content.ReadFromJsonAsync<Guid>();
        
        _trackedUserIds.Add(newUserId); // Трекаем пользователя!

        // ==========================================================
        // ШАГ 5: Логин Архитектора
        // ==========================================================
        var archLoginResponse = await _client.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = architectUser, Password = architectPass
        });
        var archAuth = await archLoginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
                _architectTokenForTeardown = archAuth.GetProperty("token").GetString();
        SetAuthorizationToken(archAuth.GetProperty("token").GetString()!);

        // ==========================================================
        // ШАГ 6: Создание IT-системы
        // ==========================================================
        var systemResponse = await _client.PostAsJsonAsync("/api/v2/systems", new CreateITSystemDto
        {
            Name = $"Billing_Service_{uniqueSuffix}", Description = "Test System", TeamId = createdTeam.Id
        });
        var sysElement = await systemResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var systemId = sysElement.GetProperty("id").GetGuid();
        
        _trackedSystemIds.Add(systemId);

        // 7. Создание компонентов и связи
        var comp1Response = await _client.PostAsJsonAsync("/api/v2/components", new CreateComponentDto(
            systemId, ComponentType.Microservice, "OrderService", "API"));
        var comp1Id = (await comp1Response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        var comp2Response = await _client.PostAsJsonAsync("/api/v2/components", new CreateComponentDto(
            systemId, ComponentType.Database, "OrderDB", "Storage"));
        var comp2Id = (await comp2Response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync("/api/v2/links", new CreateLinkDto(
            comp1Id, comp2Id, LinkSeverity.High, ProtocolType.TCP));

        // 8. Анализ
        var analysisResponse = await _client.PostAsJsonAsync("/api/v2/analysis", new AnalysisRequestDto(
            Type: AnalysisType.CascadingFailure, ComponentId: comp2Id));
        var analysisResult = await analysisResponse.Content.ReadFromJsonAsync<AnalysisResponseDto>(JsonOptions);
        analysisResult!.ImpactedComponentIds.Should().Contain(comp1Id);
    }

    private void SetAuthorizationToken(string token) => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private void ClearAuthorizationToken() => _client.DefaultRequestHeaders.Authorization = null;
}