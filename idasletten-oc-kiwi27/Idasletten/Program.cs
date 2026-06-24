using Idasletten.Shared.Auth;
using Idasletten.Shared.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Idasletten;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser());
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // In-memory SQLite for local development
            connectionString = "DataSource=idasletten-dev;mode=memory;cache=shared";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            builder.Services.AddSingleton<DbConnection>(connection);
            builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlite(sp.GetRequiredService<DbConnection>());
            });
        }
        else
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
        }

        builder.Services.AddIdentity<Features.Users.AppUser, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = false;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddIdaslettenAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddHttpClient();

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        builder.Services.AddScoped<Features.Scoring.IScoreCalculatorFactory, Features.Scoring.ScoreCalculatorFactory>();
        builder.Services.AddScoped<Features.Scoring.ITournamentRecalculator, Features.Scoring.TournamentRecalculator>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseIdaslettenForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();

        if (app.Environment.IsEnvironment("Testing"))
        {
            app.MapGet("/test-login", async (SignInManager<Features.Users.AppUser> signInManager, UserManager<Features.Users.AppUser> userManager, IConfiguration config) =>
            {
                var email = config["TestUser__Email"] ?? "test@idasletten.local";
                var user = await userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                }
                return Results.Redirect("/");
            });
        }

        await DbInitializer.SeedAsync(app.Services, app.Environment);

        app.Run();
    }
}
