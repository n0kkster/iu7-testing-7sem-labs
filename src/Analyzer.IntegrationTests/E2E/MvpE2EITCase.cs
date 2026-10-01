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

public class MvpScenarioE2EITCase : IClassFixture<FaultAnalyzerApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MvpScenarioE2EITCase(FaultAnalyzerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExecuteFullMvpScenario_FromRegistrationToAnalysis_ViaHttpApi_V2()
    {
        // ====================================================================
        // ARRANGE
        // ====================================================================
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
        var architectEmail = $"arch_{uniqueSuffix}@corp.local";
        var architectUser = $"architect_{uniqueSuffix}";
        var architectPass = "P@ssword1234!";

        // ====================================================================
        // ACT & ASSERT: ШАГ 1 — Вход администратора в систему
        // ====================================================================
        var adminLoginResponse = await _client.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = "admin",
            Password = "AdminSecret123!"
        });

        adminLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminAuth = await adminLoginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var adminToken = adminAuth.GetProperty("token").GetString();
        adminToken.Should().NotBeNullOrWhiteSpace();

        SetAuthorizationToken(adminToken!);

        // ====================================================================
        // ACT & ASSERT: ШАГ 2 — Создание команды администратором
        // ====================================================================
        var createTeamResponse = await _client.PostAsJsonAsync("/api/v2/teams", new CreateTeamDto
        {
            Name = $"Platform_Core_{uniqueSuffix}",
            Description = "Platform core architecture group"
        });

        createTeamResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTeam = await createTeamResponse.Content.ReadFromJsonAsync<TeamDto>(JsonOptions);
        createdTeam.Should().NotBeNull();
        createdTeam!.Id.Should().NotBeEmpty();

        // ====================================================================
        // ACT & ASSERT: ШАГ 3 — Выпуск инвайта для Архитектора
        // ====================================================================
        var inviteRequest = new
        {
            email = architectEmail,
            validForDays = 7,
            role = Role.Architect
        };

        var inviteResponse = await _client.PostAsJsonAsync($"/api/v2/teams/{createdTeam.Id}/invites", inviteRequest);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteDto>(JsonOptions);
        invite.Should().NotBeNull();
        invite!.Code.Should().NotBeNullOrWhiteSpace();

        // ====================================================================
        // ACT & ASSERT: ШАГ 4 — Регистрация Архитектора по инвайту
        // ====================================================================
        ClearAuthorizationToken();

        var registerResponse = await _client.PostAsJsonAsync("/api/v2/users/register", new RegisterDto
        {
            Username = architectUser,
            Email = architectEmail,
            Password = architectPass,
            InviteCode = invite.Code
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ====================================================================
        // ACT & ASSERT: ШАГ 5 — Вход Архитектора
        // ====================================================================
        var archLoginResponse = await _client.PostAsJsonAsync("/api/v2/users/login", new LoginDto
        {
            Username = architectUser,
            Password = architectPass
        });

        archLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archAuth = await archLoginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var archToken = archAuth.GetProperty("token").GetString();
        archToken.Should().NotBeNullOrWhiteSpace();

        SetAuthorizationToken(archToken!);

        // ====================================================================
        // ACT & ASSERT: ШАГ 6 — Создание IT-системы
        // ====================================================================
        var systemResponse = await _client.PostAsJsonAsync("/api/v2/systems", new CreateITSystemDto
        {
            Name = $"Billing_Service_{uniqueSuffix}",
            Description = "Processing system V2",
            TeamId = createdTeam.Id
        });

        systemResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var systemCreationResult = await systemResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var systemId = systemCreationResult.GetProperty("id").GetGuid();
        systemId.Should().NotBeEmpty();

        // ====================================================================
        // ACT & ASSERT: ШАГ 7 — Создание компонентов и связи
        // ====================================================================
        var comp1Response = await _client.PostAsJsonAsync("/api/v2/components", new CreateComponentDto(
            systemId, ComponentType.Microservice, "OrderService", "Order handling API"));
        comp1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var comp1Result = await comp1Response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var comp1Id = comp1Result.GetProperty("id").GetGuid();

        var comp2Response = await _client.PostAsJsonAsync("/api/v2/components", new CreateComponentDto(
            systemId, ComponentType.Database, "OrderDB", "Postgres orders storage"));
        comp2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var comp2Result = await comp2Response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var comp2Id = comp2Result.GetProperty("id").GetGuid();

        // Создаем зависимость: OrderService зависит от OrderDB
        var linkResponse = await _client.PostAsJsonAsync("/api/v2/links", new CreateLinkDto(
            comp1Id, comp2Id, LinkSeverity.High, ProtocolType.TCP));
        linkResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // ====================================================================
        // ACT & ASSERT: ШАГ 8 — Запуск анализа каскадного сбоя
        // ====================================================================
        var analysisRequest = new AnalysisRequestDto(
            Type: AnalysisType.CascadingFailure,
            ComponentId: comp2Id
        );

        var analysisResponse = await _client.PostAsJsonAsync("/api/v2/analysis", analysisRequest);
        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var analysisResult = await analysisResponse.Content.ReadFromJsonAsync<AnalysisResponseDto>(JsonOptions);
        analysisResult.Should().NotBeNull();
        analysisResult!.ImpactedComponentIds.Should().Contain(comp1Id);

        // ====================================================================
        // ACT & ASSERT: ШАГ 9 — Экспорт топологии системы
        // ====================================================================
        var exportResponse = await _client.GetAsync($"/api/v2/systems/{systemId}/export");
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rawExportJson = await exportResponse.Content.ReadAsStringAsync();
        rawExportJson.Should().Contain("OrderService");
        rawExportJson.Should().Contain("OrderDB");

        // ====================================================================
        // ACT & ASSERT: ШАГ 10 — Удаление системы
        // ====================================================================
        var deleteResponse = await _client.DeleteAsync($"/api/v2/systems/{systemId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getMissingSystemResponse = await _client.GetAsync($"/api/v2/systems/{systemId}");
        getMissingSystemResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SetAuthorizationToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ClearAuthorizationToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }
}