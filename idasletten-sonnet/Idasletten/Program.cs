using Idasletten.Features.Scoring;
using Idasletten.Shared.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Forwarded headers for Fly.io proxy (KnownNetworks and KnownProxies cleared so ASP.NET Core trusts Fly's proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database — SQLite in-memory locally, file-based in production
if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=idasletten.db"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=idasletten-dev.db"));
}

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Scoring calculators
builder.Services.AddScoped<EloScoreCalculator>();
builder.Services.AddScoped<TrueSkillScoreCalculator>();
builder.Services.AddScoped<LivesScoreCalculator>();
builder.Services.AddScoped<WinCountScoreCalculator>();
builder.Services.AddScoped<ScoreCalculatorFactory>();

// Authentication
var testUserEmail = builder.Configuration["TestUser:Email"];
var testUserPassword = builder.Configuration["TestUser:Password"];
var hasTestUser = !string.IsNullOrEmpty(testUserEmail) && !string.IsNullOrEmpty(testUserPassword);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
})
.AddMicrosoftIdentityWebApp(
    builder.Configuration.GetSection("AzureAd"),
    cookieScheme: null,
    openIdConnectScheme: "AzureAd");

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// Expose test user config to pages
builder.Services.AddSingleton(new TestUserConfig(hasTestUser, testUserEmail, testUserPassword));

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Apply migrations and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(db);

    if (hasTestUser)
        await DbSeeder.SeedTestUserAsync(db, testUserEmail!);
}

// Test user login (only when env vars are set)
app.MapPost("/login/test", async (HttpContext ctx, TestUserConfig cfg, string? returnUrl) =>
{
    if (!cfg.Enabled) return Results.NotFound();

    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    if (email != cfg.Email || password != cfg.Password)
        return Results.Redirect("/login?error=invalid");

    var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email) };
    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new System.Security.Claims.ClaimsPrincipal(identity));

    return Results.Redirect(returnUrl ?? "/");
});

app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.Run();

public record TestUserConfig(bool Enabled, string? Email, string? Password);
public partial class Program { }
