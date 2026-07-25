using Microsoft.EntityFrameworkCore;
using PresenceFlow.Auth;
using PresenceFlow.Components;
using PresenceFlow.Data;
using PresenceFlow.DataAccessLayer;
using PresenceFlow.Hubs;
using PresenceFlow.Middleware;
using PresenceFlow.Models;
using PresenceFlow.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<PresenceDbContext>(options =>
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

// Middleware registrieren
app.UseMiddleware<AuthCookieRefreshMiddleware>();

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

        var cookieCreated = await authCookieService.SignInAsync(email);

        if (!cookieCreated)
        {
            return Results.Redirect("/login");
        }

        var person = await presenceService.GetPersonAsync(email);

        if (person?.Status == PresenceStatus.Absent)
        {
            await presenceService.LoginAsync(person.Email);
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
