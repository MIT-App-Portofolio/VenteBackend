using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Services;

namespace Server.Pages.Admin;

public class FallbackPfp(IProfilePictureService pfpService) : PageModel
{
    public void OnGet()
    {
        
    }
    
    [BindProperty] public IFormFile File { get; set; }

    public async Task OnPostAsync()
    {
        await pfpService.UploadProfilePictureAsync(File.OpenReadStream(), "fallback");
    }
}