namespace Analyzer.BddTests.Support;

public class ScenarioDataContext
{
    public HttpClient ApiClient { get; set; } = null!;
    public string AdminToken { get; set; } = string.Empty;

    public Guid TeamId { get; set; }
    public Guid TechUserId { get; set; }
    public string TechUsername { get; set; } = string.Empty;
    public string TechEmail { get; set; } = string.Empty;

    public HttpResponseMessage? LastResponse { get; set; }
    public string? LastTwoFactorCode { get; set; }
    public string? LastUnlockCode { get; set; }
    public string? UserJwtToken { get; set; }
}