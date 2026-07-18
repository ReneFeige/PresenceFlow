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

// Services injizieren
builder.Services.AddScoped<IPresenceRepository, IoBrokerRepository>();
builder.Services.AddScoped<IPresenceService, PresenceService>();
builder.Services.AddScoped<IEmailService, AzureEmailService>();
builder.Services.AddScoped<IMagicLinkAuthService, MagicLinkAuthService>();
builder.Services.AddScoped<IAuthCookieService, AuthCookieService>();

builder.Services.AddSingleton<LoginTokenStore>();

builder.Services.AddDbContext<PresenceDbContext>(options =>
    options.UseSqlite("Data Source=presence.db"));

// SignalR hinzufügen
builder.Services.AddSignalR();

var app = builder.Build();

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
