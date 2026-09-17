namespace Analyzer.Client.Infrastructure;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using Serilog;

public class ErrorHandler(ISnackbar snackbar, NavigationManager navManager) : DelegatingHandler
{
    private record HttpErrorResponse(string Type, string Title, int Status, string Detail, string TraceId);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.Content is not null)
                    await response.Content.LoadIntoBufferAsync();

                string errorMessage;

                if (response.StatusCode == HttpStatusCode.Unauthorized) // 401
                {
                    errorMessage = "Сессия истекла или вы не авторизованы. Пожалуйста, войдите заново.";
                    navManager.NavigateTo($"/login", forceLoad: true);
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden) // 403
                {
                    errorMessage = "У вас нет прав для выполнения этой операции.";
                }
                else
                {
                    var mediaType = response.Content?.Headers.ContentType?.MediaType;
                    
                    if (mediaType is not null && 
                        (mediaType.Contains("application/json") || mediaType.Contains("problem+json")))
                    {
                        try
                        {
                            var errorContent = await response.Content!
                                .ReadFromJsonAsync<HttpErrorResponse>(cancellationToken: cancellationToken);
                            
                            errorMessage = 
                                errorContent?.Detail ?? 
                                errorContent?.Title ?? 
                                $"Ошибка сервера: {response.StatusCode}";
                        }
                        catch (JsonException ex)
                        {
                            Log.Error(ex, "Ошибка парсинга ответа.");
                            errorMessage = $"Ошибка сервера: {response.StatusCode}";
                        }
                    }
                    else
                    {
                        var rawContent = await response.Content!.ReadAsStringAsync(cancellationToken);
                        errorMessage = string.IsNullOrWhiteSpace(rawContent) 
                            ? $"Ошибка сервера: {(int)response.StatusCode}" 
                            : $"Ошибка {(int)response.StatusCode}: {rawContent}";
                    }
                }

                snackbar.Add(errorMessage, Severity.Error, config =>
                {
                    config.ShowCloseIcon = true;
                    config.RequireInteraction = true;
                });
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Нет связи с сервером.");
            snackbar.Add("Нет связи с сервером. Проверьте подключение.", Severity.Error);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Неизвестная ошибка");
            throw;
        }
    }
}