using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Server.Data;

namespace Server.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public Models.LoginModel Input { get; set; }

        public string? ReturnUrl { get; set; }

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return LocalRedirect(ReturnUrl ?? "/");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}