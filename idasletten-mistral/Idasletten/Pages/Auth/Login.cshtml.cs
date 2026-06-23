using Idasletten.Features.Users;
using Idasletten.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public LoginModel(
        IConfiguration configuration,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _configuration = configuration;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public bool ShowTestUserLogin { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        // Test user is always shown for development
        var testUserEmail = _configuration["TestUser__Email"];
        var testUserPassword = _configuration["TestUser__Password"];
        ShowTestUserLogin = !string.IsNullOrEmpty(testUserEmail) && !string.IsNullOrEmpty(testUserPassword);
    }

    public async Task<IActionResult> OnPostAsync(string? useTestUser, string? Email, string? Password)
    {
        if (useTestUser == "true")
        {
            // Test user login
            try
            {
                var testUserEmail = _configuration["TestUser__Email"];
                var testUserPassword = _configuration["TestUser__Password"];

                if (string.IsNullOrEmpty(testUserEmail) || string.IsNullOrEmpty(testUserPassword))
                {
                    ErrorMessage = "Test bruger er ikke konfigureret";
                    ShowTestUserLogin = true;
                    return Page();
                }

                // Validate provided credentials
                if (Email != testUserEmail || Password != testUserPassword)
                {
                    ErrorMessage = "Forkert email eller adgangskode";
                    ShowTestUserLogin = true;
                    return Page();
                }

                // Find or create test user
                var user = await _userManager.FindByEmailAsync(testUserEmail);
                
                if (user == null)
                {
                    // Create test user if not exists
                    user = new User
                    {
                        UserName = testUserEmail,
                        Email = testUserEmail,
                        Name = "Test Bruger",
                        EmailConfirmed = true
                    };
                    
                    var createResult = await _userManager.CreateAsync(user, testUserPassword);
                    if (!createResult.Succeeded)
                    {
                        ErrorMessage = "Fejl ved oprettelse af test bruger";
                        return Page();
                    }
                }

                // Sign in
                var signInResult = await _signInManager.SignInAsync(user, false);
                
                if (signInResult.Succeeded)
                {
                    return RedirectToPage("/Index");
                }
                else
                {
                    ErrorMessage = "Fejl ved login: " + string.Join(", ", signInResult.Errors);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        ShowTestUserLogin = !string.IsNullOrEmpty(_configuration["TestUser__Email"]);
        
        return Page();
    }
}
