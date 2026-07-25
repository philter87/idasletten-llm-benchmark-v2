using Idasletten.Shared.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdasletten(builder.Configuration);
// Migrations are applied - and an empty database seeded - while the host starts.
builder.Services.AddDatabaseInitialisation();
builder.Services.AddRazorPages(options =>
{
    // Creating a tournament is the only page that requires a login. Recording a result does not.
    options.Conventions.AuthorizePage("/Tournaments/Create");
});

var app = builder.Build();

// Fly.io terminates TLS in front of us - trust its forwarded headers before anything else runs.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Plain static files, not MapStaticAssets: the fingerprinted asset endpoints answered compressed
// requests with an empty body, which left the pages completely unstyled.
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

/// <summary>Exposed so the test project can boot the real application with WebApplicationFactory.</summary>
public partial class Program;
