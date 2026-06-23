using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LoginModel(TestUserConfig testUserConfig) : PageModel
{
    public bool TestUserEnabled => testUserConfig.Enabled;
    public string? TestEmail => testUserConfig.Email;
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl ?? "/";
    }
}
