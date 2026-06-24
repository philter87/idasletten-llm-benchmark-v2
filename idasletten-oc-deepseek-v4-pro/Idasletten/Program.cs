using System.Security.Claims;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorPages();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "DataSource=idasletten.db"));

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrEmpty(azureAdClientId))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "OpenIdConnect";
    })
    .AddCookie()
    .AddOpenIdConnect("OpenIdConnect", options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0";
        options.ClientId = azureAdClientId;
        options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.TokenValidationParameters.NameClaimType = "name";
    });
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Login";
            options.AccessDeniedPath = "/Login";
        });
}

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.AutoMigrateAsync();
}

var configureTestUser = !string.IsNullOrEmpty(app.Configuration["TestUser:Email"])
    && !string.IsNullOrEmpty(app.Configuration["TestUser:Password"]);

if (configureTestUser)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var testEmail = app.Configuration["TestUser:Email"]!;
    var existingTestUser = db.Users.FirstOrDefault(u => u.Email == testEmail);
    if (existingTestUser == null)
    {
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "TST",
            Name = "Test User",
            Email = testEmail
        });
        await db.SaveChangesAsync();
    }
}

app.Run();

public partial class Program { }
