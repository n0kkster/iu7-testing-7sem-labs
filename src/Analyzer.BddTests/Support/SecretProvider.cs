namespace Analyzer.BddTests.Support;

using Microsoft.Extensions.Configuration;

public static class SecretProvider
{
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddUserSecrets(typeof(SecretProvider).Assembly, optional: true)
        .AddEnvironmentVariables()
        .Build();

    public static string AdminUsername => Config["ADMIN_USERNAME"] ?? "admin";
    public static string AdminPassword => GetRequired("ADMIN_PASSWORD");
    public static string TechUserPassword => GetRequired("BDD_TECH_PASSWORD");
    public static string TechUserNewPassword => GetRequired("BDD_TECH_NEW_PASSWORD");

    private static string GetRequired(string key)
    {
        var value = Config[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Секрет '{key}' не задан в переменных окружения или dotnet user-secrets.");
        return value;
    }
}