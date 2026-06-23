using Idasletten.Features.Users;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add Scoring Service
builder.Services.AddScoped<IScoringService, ScoringService>();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=:memory:";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString == "Data Source=:memory:")
    {
        // In-memory database for development
        options.UseSqlite(connectionString);
    }
    else
    {
        // File-based SQLite for production
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
        });
    }
});

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<AppDbContext>();

// Authentication - Azure AD (commented out for now, needs Microsoft.Identity.Web package)
// var azureAdConfig = builder.Configuration.GetSection("AzureAd");
// if (azureAdConfig.Exists &&
//     !string.IsNullOrEmpty(azureAdConfig["ClientId"]) &&
//     !string.IsNullOrEmpty(azureAdConfig["TenantId"]))
// {
//     builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");
//     builder.Services.AddMicrosoftIdentityUI();
// }

// Test user authentication
var testUserEmail = builder.Configuration["TestUser__Email"];
var testUserPassword = builder.Configuration["TestUser__Password"];

if (!string.IsNullOrEmpty(testUserEmail) && !string.IsNullOrEmpty(testUserPassword))
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
        });
}

// Forwarded headers for Fly.io
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("CanCreateTournament", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("CanEditCompletedMatch", policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Forwarded headers for Fly.io
app.UseForwardedHeaders();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        if (connectionString != "Data Source=:memory:")
        {
            // For file-based database, apply migrations
            context.Database.Migrate();
        }
        else
        {
            // For in-memory database, ensure database is created and migrations are applied
            context.Database.EnsureCreated();
            
            // Note: In-memory SQLite doesn't support migrations in the traditional sense
            // For development, we use EnsureCreated which creates the schema
        }
        
        // Seed test user if configured
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        
        if (!string.IsNullOrEmpty(testUserEmail) && !string.IsNullOrEmpty(testUserPassword))
        {
            await SeedTestUser(userManager);
        }
        
        async Task SeedTestUser(UserManager<User> userManager)
        {
            if (!await userManager.Users.AnyAsync(u => u.Email == testUserEmail))
            {
                var testUser = new User
                {
                    UserName = testUserEmail,
                    Email = testUserEmail,
                    Name = "Test User",
                    EmailConfirmed = true
                };
                
                await userManager.CreateAsync(testUser, testUserPassword);
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
