using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Scoring;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=:memory:";
    
    options.UseSqlite(connectionString);
    
    // Enable sensitive data logging for development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// Add Identity with custom User type
builder.Services.AddIdentity<User, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure Cookie Authentication for test user
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Idasletten.Cookies";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add Azure AD Authentication as an additional scheme
var azureAdConfig = builder.Configuration.GetSection("AzureAd");

if (azureAdConfig.Exists() && !string.IsNullOrEmpty(azureAdConfig["ClientId"]))
{
    builder.Services.AddAuthentication()
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}

// Add MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add Scoring Systems
builder.Services.AddScoped<IScoringSystemFactory, ScoringSystemFactory>();
builder.Services.AddScoped<IEloScoringSystem, EloScoringSystem>();
builder.Services.AddScoped<ITrueSkillScoringSystem, TrueSkillScoringSystem>();
builder.Services.AddScoped<ILivesScoringSystem, LivesScoringSystem>();
builder.Services.AddScoped<IWinCountScoringSystem, WinCountScoringSystem>();

// Add Azure AD Authentication as an additional scheme
var azureAdConfig = builder.Configuration.GetSection("AzureAd");

if (azureAdConfig.Exists() && !string.IsNullOrEmpty(azureAdConfig["ClientId"]))
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie()
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}

// Add forwarded headers for Fly.io
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Use forwarded headers (must be before UseHttpsRedirection and UseAuthentication)
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Auto-apply migrations on startup
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // For in-memory database, we need to recreate it each time
    if (dbContext.Database.GetConnectionString() == "Data Source=:memory:")
    {
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.Migrate();
    }
    
    // Seed initial data
    SeedData.SeedAsync(scope.ServiceProvider).Wait();
}

app.Run();

// Seed data helper
public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Check if any data exists
        if (await dbContext.Users.AnyAsync())
            return;
        
        // Seed a test admin user if test user credentials are configured
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var testEmail = config["TestUser__Email"];
        var testPassword = config["TestUser__Password"];
        
        if (!string.IsNullOrEmpty(testEmail) && !string.IsNullOrEmpty(testPassword))
        {
            // Test user will be created on first login
        }
    }
}
