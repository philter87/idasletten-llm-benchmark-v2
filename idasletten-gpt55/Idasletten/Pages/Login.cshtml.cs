using Idasletten.Shared.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LoginModel(IConfiguration configuration) : PageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public bool IsTestLoginEnabled => AuthExtensions.TestLoginEnabled(configuration);
    public bool IsAzureLoginEnabled => AuthExtensions.AzureLoginEnabled(configuration);
    public string? Error { get; private set; }

    public void OnGet() { }

    public IActionResult OnPostMicrosoft(string? returnUrl = null) => Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" }, "AzureAD");

    public async Task<IActionResult> OnPostTestAsync(string? returnUrl = null)
    {
        if (!IsTestLoginEnabled || !string.Equals(Email, configuration["TestUser:Email"], StringComparison.OrdinalIgnoreCase) || Password != configuration["TestUser:Password"])
        {
            Error = "Invalid test-user credentials.";
            return Page();
        }
        await AuthExtensions.SignInTestUserAsync(HttpContext, Email);
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
