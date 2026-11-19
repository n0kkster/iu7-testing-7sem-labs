namespace Analyzer.BddTests.Support;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

public class MailpitClient(string baseUrl)
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(baseUrl) };

    public async Task ClearMessagesAsync()
    {
        await _http.DeleteAsync("/api/v1/messages");
    }

    public async Task<string> WaitForSixDigitCodeAsync(string recipientEmail, string subjectSubstring, int timeoutSeconds = 10)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var listResponse = await _http.GetFromJsonAsync<JsonElement>("/api/v1/messages");
            if (listResponse.TryGetProperty("messages", out var messages))
            {
                foreach (var msg in messages.EnumerateArray())
                {
                    var subject = msg.GetProperty("Subject").GetString() ?? "";
                    var toList = msg.GetProperty("To").EnumerateArray()
                        .Select(t => t.GetProperty("Address").GetString())
                        .ToList();

                    if (toList.Contains(recipientEmail, StringComparer.OrdinalIgnoreCase) &&
                        subject.Contains(subjectSubstring, StringComparison.OrdinalIgnoreCase))
                    {
                        var messageId = msg.GetProperty("ID").GetString()!;
                        var fullMessage = await _http.GetFromJsonAsync<JsonElement>($"/api/v1/message/{messageId}");
                        var bodyText = fullMessage.GetProperty("Text").GetString() ?? "";

                        var match = Regex.Match(bodyText, @"\b(\d{6})\b");
                        if (match.Success)
                            return match.Groups[1].Value;
                    }
                }
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Письмо с темой '{subjectSubstring}' для '{recipientEmail}' не пришло за {timeoutSeconds} сек.");
    }
}