using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Tournaments/Create");
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddScoped<ScoreCalculator>();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("Idasletten");
if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<IdaslettenDbContext>(options =>
        options.UseInMemoryDatabase("idasletten-local"));
}
else
{
    builder.Services.AddDbContext<IdaslettenDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=idasletten.db"));
}

var authentication = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/Login");

var azureClientId = builder.Configuration["AzureAd:ClientId"];
var azureTenantId = builder.Configuration["AzureAd:TenantId"];
if (!string.IsNullOrWhiteSpace(azureClientId) && !string.IsNullOrWhiteSpace(azureTenantId))
{
    authentication.AddOpenIdConnect("AzureAD", options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{azureTenantId}/v2.0";
        options.ClientId = azureClientId;
        options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
        options.CallbackPath = "/signin-oidc";
        options.ResponseType = "code";
        options.SaveTokens = true;
    });
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
    await SeedData.EnsureSeededAsync(db, app.Configuration);
}

app.MapRazorPages();
app.Run();

public partial class Program;
