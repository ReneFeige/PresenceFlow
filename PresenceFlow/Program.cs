using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PresenceFlow.Auth;
using PresenceFlow.Components;
using PresenceFlow.Data;
using PresenceFlow.DataAccessLayer;
using PresenceFlow.Hubs;
using PresenceFlow.Models;
using PresenceFlow.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Authentifizierung aktivieren und Cookies als Standard-Methode setzen
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PresenceFlowAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        // User umleiten
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";

        // Session im Hintergrund prüfen (bei jedem Seitenaufruf)
        options.Events.OnValidatePrincipal = async context =>
        {
            // Daten (Claims) aus dem verschlüsselten Cookie auslesen
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);

            var authVersionValue = context.Principal?.FindFirstValue("auth_version");

            // Falls Daten im Cookie manipuliert wurden oder fehlen: Sofort ausloggen
            if (string.IsNullOrWhiteSpace(email) || !int.TryParse(authVersionValue, out var authVersion))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            var repository = context.HttpContext.RequestServices.GetRequiredService<IPresenceRepository>();

            var person = await repository.GetPersonAsync(email);

            // Session-Sperre: Wenn User gelöscht oder AuthVersion in DB erhöht wurde (Logout auf anderem Gerät)
            if (person == null || person.AuthVersion != authVersion)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// Aktiviert das Berechtigungssystem ([Authorize] Attribute)
builder.Services.AddAuthorization();

// Stellt den Login-Status global allen Blazor-Komponenten bereit
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddDbContextFactory<PresenceDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection fehlt.");

    options.UseSqlite(connectionString);
});

var storageProvider =
    builder.Configuration["Storage:Provider"]
    ?? throw new InvalidOperationException(
        "Storage:Provider fehlt.");

if (storageProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IPresenceRepository, SQLiteRepository>();
}
else if (storageProvider.Equals("IoBroker", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IPresenceRepository, IoBrokerRepository>();
}
else
{
    throw new InvalidOperationException(
        $"Unbekannter Storage-Provider: '{storageProvider}'. " +
        "Erlaubte Werte sind 'SQLite' und 'IoBroker'.");
}

// Services injizieren
builder.Services.AddScoped<IPresenceService, PresenceService>();
builder.Services.AddScoped<IEmailService, AzureEmailService>();
builder.Services.AddScoped<IMagicLinkAuthService, MagicLinkAuthService>();
builder.Services.AddScoped<IAuthCookieService, AuthCookieService>();

builder.Services.AddSingleton<LoginTokenStore>();

// SignalR hinzufügen
builder.Services.AddSignalR();

var app = builder.Build();

await app.InitializePresenceDatabaseAsync(storageProvider);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet(
    "/auth/magic",
    async (
        string? token,
        IMagicLinkAuthService magicLinkAuthService,
        IAuthCookieService authCookieService,
        IPresenceService presenceService) =>
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Redirect("/login");
        }

        if (!magicLinkAuthService.ConsumeToken(token, out var email))
        {
            return Results.Redirect("/login");
        }

        var signedIn = await authCookieService.SignInAsync(email);

        if (!signedIn)
        {
            return Results.Redirect("/login");
        }

        var person = await presenceService.GetPersonAsync(email);

        if (person?.Status == PresenceStatus.Absent)
        {
            await presenceService.SetPresenceForTrustedEmailAsync(email, PresenceStatus.Present);
        }

        return Results.Redirect("/");
    });

app.MapGet(
    "/auth/logout",
    async (
        IAuthCookieService authCookieService) =>
    {
        await authCookieService.SignOutAsync();

        return Results.Redirect("/");
    });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hub registrieren
app.MapHub<PresenceHub>("/presenceHub");

app.Run();
