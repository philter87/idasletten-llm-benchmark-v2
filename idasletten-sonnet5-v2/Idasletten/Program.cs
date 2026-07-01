using Idasletten.Data;
using Idasletten.Shared.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TestUserOptions>(builder.Configuration.GetSection("TestUser"));

var connectionString = builder.Configuration.GetConnectionString("Default");
if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(connectionString))
{
    // SQLite in-memory mode locally: a single kept-alive connection so the schema
    // and seeded data survive for the lifetime of the app (migrations still apply).
    var keepAliveConnection = new SqliteConnection("DataSource=:memory:");
    keepAliveConnection.Open();
    builder.Services.AddSingleton(keepAliveConnection);
    builder.Services.AddDbContext<IdaslettenDbContext>((sp, options) =>
        options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
}
else
{
    builder.Services.AddDbContext<IdaslettenDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=idasletten.db"));
}

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Cookies authenticate every incoming request (including test-user sign-in) and are also the
// default *challenge* scheme, so [Authorize] redirects unauthenticated users to our own
// /Login page rather than straight to Azure AD. OpenIdConnect is only triggered explicitly,
// when the "Log in with Microsoft" button on /Login issues its own Challenge.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.GetSection("AzureAd").Bind(options);
        options.Events.OnTokenValidated = AzureAdUserProvisioning.OnTokenValidated;
    });

builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options => options.LoginPath = "/Login");

builder.Services.AddAuthorization();
builder.Services.AddRazorPages(options =>
    {
        // @page route templates are appended to the file-based path by default (e.g.
        // "/Tournaments/CreateMatch/{id}/create-match"); these conventions replace them
        // with the flat URLs from the spec (e.g. "/tournaments/{id}/create-match").
        options.Conventions.AddPageRoute("/Tournaments/Details", "tournaments/{id:guid}");
        options.Conventions.AddPageRoute("/Tournaments/CreateMatch", "tournaments/{id:guid}/create-match");
        options.Conventions.AddPageRoute("/Tournaments/Matches", "tournaments/{id:guid}/matches");
        options.Conventions.AddPageRoute("/Tournaments/Players", "tournaments/{id:guid}/players");
        options.Conventions.AddPageRoute("/Users/Details", "users/{id:guid}");
    })
    .AddMicrosoftIdentityUI();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Fly.io's proxy isn't on a known network, so trust all forwarders.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
    db.Database.Migrate();
    var testUserOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestUserOptions>>().Value;
    var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
    await DataSeeder.SeedAsync(db, sender, testUserOptions);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;
