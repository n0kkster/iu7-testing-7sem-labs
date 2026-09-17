using MudBlazor.Services;
using Analyzer.Client.Components;
using Analyzer.Client.Services;
using Analyzer.Client.Providers;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

using Serilog;
using Analyzer.Client.Infrastructure;
using Microsoft.AspNetCore.Authentication;

// ============================================================================
// 🔐 НАСТРОЙКА ЛОГГЕРА
// ============================================================================
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
try
{
    Log.Information("🚀 Запуск приложения Analyzer.Client");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog();

    // ============================================================================
    // 🔐 1. БАЗОВЫЕ СЕРВИСЫ
    // ============================================================================
    
    // Доступ к HttpContext в компонентах и сервисах
    builder.Services.AddHttpContextAccessor();

    // ============================================================================
    // 🔐 2. АУТЕНТИФИКАЦИЯ И АВТОРИЗАЦИЯ
    // ============================================================================
    
    // Cookie-аутентификация для Blazor UI
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = ".Analyzer.Auth";
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
            
            options.ExpireTimeSpan = TimeSpan.FromHours(2);
            options.SlidingExpiration = true;
            
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.IsEssential = true;
        });

    // Базовая настройка авторизации
    builder.Services.AddAuthorizationCore();

    // 🔹 CascadingAuthenticationState — обязателен для работы [Authorize] в компонентах
    builder.Services.AddCascadingAuthenticationState();

    // ============================================================================
    // 🔐 3. КАСТОМНЫЕ СЕРВИСЫ АУТЕНТИФИКАЦИИ
    // ============================================================================
    
    // AuthStateProvider — читает состояние из HttpContext (сервер)
    builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
    builder.Services.AddScoped(sp => 
        (AuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

    // AuthService — логика логина/логаута
    builder.Services.AddScoped<IAuthService, AuthService>();

    // ============================================================================
    // 🔐 4. HTTP CLIENT С ЦЕПОЧКОЙ ОБРАБОТЧИКОВ
    // ============================================================================
    
    // ErrorHandler — глобальная обработка ошибок HTTP
    builder.Services.AddScoped<ErrorHandler>();
    
    // JwtAuthorizationHandler — добавляет Bearer-токен к запросам к API
    builder.Services.AddScoped<JwtAuthorizationHandler>();

    // 🔹 Настройка HttpClient с цепочкой: HttpClient → ErrorHandler → JwtHandler → HttpClientHandler
    builder.Services.AddScoped(sp =>
    {
        var errorHandler = sp.GetRequiredService<ErrorHandler>();
        var jwtHandler = sp.GetRequiredService<JwtAuthorizationHandler>();
        
        // Базовый обработчик с настройками куки
        var baseHandler = new HttpClientHandler
        {
            UseCookies = true,
            AllowAutoRedirect = true,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        
        // Выстраиваем цепочку: последний в цепочке — baseHandler
        jwtHandler.InnerHandler = baseHandler;
        errorHandler.InnerHandler = jwtHandler;

        return new HttpClient(errorHandler)
        {
            BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:1555"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    });

    // ============================================================================
    // 🔐 5. UI И RAZOR COMPONENTS
    // ============================================================================
    
    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
        config.SnackbarConfiguration.PreventDuplicates = true;
        config.SnackbarConfiguration.NewestOnTop = true;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 3000;
        config.SnackbarConfiguration.HideTransitionDuration = 200;
        config.SnackbarConfiguration.ShowTransitionDuration = 200;
    });

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // ============================================================================
    // 🚀 BUILD & CONFIGURE APP
    // ============================================================================
    
    var app = builder.Build();

    // ============================================================================
    // 🔐 6. MIDDLEWARE PIPELINE
    // ============================================================================
    
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    
    // Статические файлы
    app.MapStaticAssets();
    
    // 🔹 Routing должен быть ДО сессии и аутентификации
    app.UseRouting();
        
    // 🔹 Аутентификация — читает куку, создаёт HttpContext.User
    app.UseAuthentication();
    
    // 🔹 Авторизация — проверяет [Authorize] на основе HttpContext.User
    app.UseAuthorization();
    
    // 🔹 Anti-forgery защита для форм и POST-запросов
    app.UseAntiforgery();

    // ============================================================================
    // 🔐 7. ENDPOINTS
    // ============================================================================
    
    // Razor Components с интерактивным серверным рендерингом
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Эндпоинт для логаута
    app.MapGet("/logout", async (HttpContext ctx) =>
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    });

    // ============================================================================
    // 🚀 RUN
    // ============================================================================
    
    Log.Information("✅ Приложение готово к запуску на {Url}", 
        builder.Configuration["Urls"] ?? "http://localhost:1777");
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Критическая ошибка при запуске приложения");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
