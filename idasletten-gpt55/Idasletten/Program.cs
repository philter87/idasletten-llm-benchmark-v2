using Idasletten.Shared.Auth;
using Idasletten.Shared.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Tournaments/Create");
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var useInMemorySqlite = builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default"));
if (useInMemorySqlite)
{
    var connection = new SqliteConnection("Data Source=Idasletten;Mode=Memory;Cache=Shared");
    connection.Open();
    builder.Services.AddSingleton(connection);
    builder.Services.AddDbContext<IdaslettenDbContext>(options => options.UseSqlite(connection));
}
else
{
    var databasePath = builder.Configuration.GetConnectionString("Default") ?? "Data Source=/data/idasletten.db";
    builder.Services.AddDbContext<IdaslettenDbContext>(options => options.UseSqlite(databasePath));
}

var auth = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
});

if (AuthExtensions.AzureLoginEnabled(builder.Configuration))
{
    auth.AddOpenIdConnect("AzureAD", options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0";
        options.ClientId = builder.Configuration["AzureAd:ClientId"];
        options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

var forwardedHeaders = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
    await db.Database.MigrateAsync();
}
await SeedData.SeedAsync(app.Services);

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
app.MapRazorPages().WithStaticAssets();
app.Run();

public partial class Program;
