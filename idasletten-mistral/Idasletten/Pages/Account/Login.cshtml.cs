using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Idasletten.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;
    
    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [BindProperty]
    public string Email { get; set; } = default!;
    
    [BindProperty]
    public string Password { get; set; } = default!;
    
    public string? ReturnUrl { get; set; }
    public string? ExternalLoginError { get; set; }
    
    public bool TestUserEnabled => 
        !string.IsNullOrEmpty(_configuration["TestUser__Email"]) && 
        !string.IsNullOrEmpty(_configuration["TestUser__Password"]);
    
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            ReturnUrl = returnUrl;
        }
        
        // Check for external login errors
        if (await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme) is { } result
            && result.Succeeded is false
            && result.Failure != null)
        {
            ExternalLoginError = result.Failure.Message;
        }
        
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Check if this is a test user login
        var testEmail = _configuration["TestUser__Email"];
        var testPassword = _configuration["TestUser__Password"];
        
        if (TestUserEnabled && !string.IsNullOrEmpty(Request.Form["testLogin"]))
        {
            // Validate test user credentials
            if (Email == testEmail && Password == testPassword)
            {
                // Create claims for test user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Name, Email),
                    new Claim(ClaimTypes.Email, Email),
                    new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "test-user-id"),
                    new Claim("http://schemas.microsoft.com/identity/claims/identityprovider", "Test")
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, "TestAuthentication");
                var principal = new ClaimsPrincipal(claimsIdentity);
                
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                    });
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                
                return LocalRedirect("/");
            }
            
            ModelState.AddModelError(string.Empty, "Invalid email or password for test user");
        }
        
        // Handle Azure AD login challenge
        var provider = Request.Form["provider"].ToString();
        if (provider == "Microsoft")
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Account/Login", new { returnUrl }),
                Items = { { "LoginProvider", provider } }
            };
            
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }
        
        return Page();
    }
}
