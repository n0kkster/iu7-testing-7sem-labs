using System.Security.Claims;
using System.Text.Json;

namespace Analyzer.Client.Utils;

public static class JwtHelper
{
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        if (string.IsNullOrEmpty(jwt) || jwt.Split('.').Length != 3)
            return [];

        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        
        try
        {
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
            if (keyValuePairs == null) 
                return [];

            var claims = new List<Claim>();
            
            foreach (var kvp in keyValuePairs)
            {
                var claimType = kvp.Key switch
                {
                    "sub" or "nameid" or "name_id" => ClaimTypes.NameIdentifier,
                    "name" => ClaimTypes.Name,
                    "email" => ClaimTypes.Email,
                    "role" or "roles" => ClaimTypes.Role,
                    _ => kvp.Key
                };

                var claimValue = kvp.Value.ValueKind switch
                {
                    JsonValueKind.Array => kvp.Value.EnumerateArray()
                        .Select(v => new Claim(claimType, v.ToString())),
                    _ => [new Claim(claimType, kvp.Value.ToString() ?? string.Empty)]
                };

                claims.AddRange(claimValue);
            }

            return claims;
        }
        catch
        {
            return [];
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}