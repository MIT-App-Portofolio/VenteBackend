using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;
using Server.Services;

namespace Server.Pages.Admin;

public class CreateFakeUser(UserManager<ApplicationUser> userManager, IProfilePictureService profilePictureService) : PageModel
{
    public class CreateFakeUserInput
    {
        public string UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? Ig { get; set; }
        public string? Description { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime EventStatusDate { get; set; }
        public Gender Gender { get; set; }
        public IFormFile ProfilePicture { get; set; }
    }
    
    public void OnGet() { }
   
    [BindProperty]
    public CreateFakeUserInput Input { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new ApplicationUser
        {
            UserName = Input.UserName,
            Email = "user_" + Input.UserName + "@example.com",
            Name = Input.DisplayName,
            IgHandle = Input.Ig,
            Description = Input.Description,
            BirthDate = Input.BirthDate,
            Blocked = [],
            Gender = Input.Gender,
            EventStatus = new EventStatus
            {
                Active = true,
                Location = Location.Salou,
                Time = Input.EventStatusDate
            },
            HasPfp = true
        };

        var result = await userManager.CreateAsync(user);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await profilePictureService.UploadProfilePictureAsync(Input.ProfilePicture.OpenReadStream(), user.UserName);

        return RedirectToPage("/Admin/Users");
    }
}