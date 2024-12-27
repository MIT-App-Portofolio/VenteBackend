using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Server.Data;

namespace Server.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public Models.RegisterModel Input { get; set; }

        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser { UserName = Input.UserName, Email = Input.Email, Gender = Input.Gender };
            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}