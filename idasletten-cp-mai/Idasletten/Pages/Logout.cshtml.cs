using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LogoutModel : PageModel
{
    private readonly SignInManager<Features.Users.AppUser> _signInManager;

    public LogoutModel(SignInManager<Features.Users.AppUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        await HttpContext.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
