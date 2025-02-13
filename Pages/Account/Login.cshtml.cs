using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Server.Data;

namespace Server.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
                return LocalRedirect(ReturnUrl ?? "/");

            ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión.");
            return Page();
        }
    }
}