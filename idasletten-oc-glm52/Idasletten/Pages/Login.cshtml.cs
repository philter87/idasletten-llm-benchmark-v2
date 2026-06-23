using Idasletten.Features.Users;
using Idasletten.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly IdaslettenDbContext _db;
    private readonly IConfiguration _cfg;

    public LoginModel(IdaslettenDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    public bool ShowTestLogin { get; private set; }
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ShowTestLogin = !string.IsNullOrEmpty(_cfg["TestUser__Password"])
                        && !string.IsNullOrEmpty(_cfg["TestUser__Email"]);
        ReturnUrl = returnUrl;
    }

    public IActionResult OnPostMicrosoft(string? returnUrl = null)
        => Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/", Items = { ["scheme"] = "Microsoft" } }, "Microsoft");

    public async Task<IActionResult> OnPostTestAsync(string? returnUrl = null)
    {
        var email = _cfg["TestUser__Email"];
        var password = _cfg["TestUser__Password"];
        var username = _cfg["TestUser__Username"] ?? "TST";
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return RedirectToPage("/Login");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username)
                   ?? new User { Username = username, Name = $"Test User {username}", Email = email };
        if (user.Id == Guid.Empty) { _db.Users.Add(user); await _db.SaveChangesAsync(); }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email ?? email),
            new Claim("username", user.Username),
        }, "Idasletten");
        await HttpContext.SignInAsync("Idasletten", new ClaimsPrincipal(identity));
        return LocalRedirect(returnUrl ?? "/");
    }
}