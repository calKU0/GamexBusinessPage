using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty]
    [Required(ErrorMessage = "Podaj hasło administratora.")]
    public string Password { get; set; } = string.Empty;

    public void OnGet()
    {
        ViewData["Title"] = "Panel administracyjny - logowanie";
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ViewData["Title"] = "Panel administracyjny - logowanie";

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var expectedPassword = _configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(expectedPassword) || !string.Equals(expectedPassword, Password, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Nieprawidłowe hasło administratora.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Admin/Index");
    }
}
