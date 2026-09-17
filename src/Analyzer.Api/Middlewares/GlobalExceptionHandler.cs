using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Analyzer.Api.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException && 
            exception is not InvalidOperationException && 
            exception is not KeyNotFoundException &&
            exception is not UnauthorizedAccessException)
        {
            var requestPath = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            Log.Error(exception, "Критическая ошибка при выполнении запроса: {RequestPath}", requestPath);
        }

        var (statusCode, title) = MapExceptionToStatusCode(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError 
                        ? "Произошла непредвиденная внутренняя ошибка сервера." 
                        : exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title) MapExceptionToStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Ошибка валидации данных"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Нарушение бизнес-правила"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Ресурс не найден"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Доступ запрещен"),
            
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };
    }
}