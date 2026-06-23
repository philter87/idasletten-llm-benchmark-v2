using Idasletten;
using Idasletten.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<IdaslettenDbContext>());
builder.Services.AddHttpContextAccessor();

// Database: SQLite in-memory locally (Development), file-based in Production.
var dbConn = builder.Configuration.GetConnectionString("Sqlite")
    ?? (builder.Environment.IsDevelopment()
        ? "DataSource=:memory:"
        : "DataSource=/data/idasletten.db");

if (builder.Environment.IsDevelopment())
{
    // In-memory SQLite needs the connection kept open for the lifetime of the app.
    builder.Services.AddSingleton<SqliteConnectionHolder>(_ => new SqliteConnectionHolder(dbConn));
    builder.Services.AddDbContext<IdaslettenDbContext>((sp, opts) =>
    {
        var holder = sp.GetRequiredService<SqliteConnectionHolder>();
        opts.UseSqlite(holder.Connection);
    });
}
else
{
    builder.Services.AddDbContext<IdaslettenDbContext>(opts => opts.UseSqlite(dbConn));
}

// Forwarded headers for Fly.io proxy so Azure AD redirects use https://
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = "Idasletten";
    o.DefaultSignInScheme = "Idasletten";
})
.AddCookie("Idasletten", o =>
{
    o.LoginPath = "/login";
    o.AccessDeniedPath = "/login";
});

var azureAd = builder.Configuration.GetSection("AzureAd");
if (azureAd.Exists() && !string.IsNullOrEmpty(azureAd["ClientId"]))
{
    builder.Services.AddAuthentication()
        .AddMicrosoftAccount("Microsoft", o =>
        {
            o.ClientId = azureAd["ClientId"]!;
            o.ClientSecret = azureAd["ClientSecret"];
            o.SaveTokens = false;
        });
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
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

// Apply migrations automatically on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program { }