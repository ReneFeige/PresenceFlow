using PresenceFlow.Auth;
using PresenceFlow.Components;
using PresenceFlow.DataAccessLayer;
using PresenceFlow.Hubs;
using PresenceFlow.Middleware;
using PresenceFlow.Services;
using PresenceFlow.Data;
using Microsoft.EntityFrameworkCore;

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

if (storageProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider.GetRequiredService<PresenceDbContext>();

    await dbContext.Database.MigrateAsync();

    await PresenceDbSeeder.SeedAsync(dbContext);
}

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hub registrieren
app.MapHub<PresenceHub>("/presenceHub");

app.Run();
