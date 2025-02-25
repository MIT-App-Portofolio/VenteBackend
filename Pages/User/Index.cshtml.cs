using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;

namespace Server.Pages.User;

public class Index(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
    public string UserName { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        Name = user.Name;
        UserName = user.UserName;
        Email = user.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        var user = await userManager.GetUserAsync(User);
        await userManager.DeleteAsync(user);
        await signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}