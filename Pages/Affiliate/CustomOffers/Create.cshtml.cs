using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Interfaces;
using System.Text.Json;

namespace Server.Pages.Affiliate.CustomOffers;

public class CustomOfferUserDisplay
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public DateTimeOffset EventDate { get; set; }
    public string? PfpUrl { get; set; }
    public DateTimeOffset? BirthDate { get; set; }
    public Gender Gender { get; set; }
}

public class CreateOfferModel
{
    public string Name { get; set; }
    public string? Description { get; set; }

    public IFormFile? Picture { get; set; }

    public DateTimeOffset ValidUntil { get; set; }
    
    public string TargetUsersJson { get; set; }
}

public class Create(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IProfilePictureService pfpService, ICustomOfferPictureService offerPictureService) : PageModel
{
    public void OnGet() { } 
    
    public async Task<IActionResult> OnGetUsersAsync()
    {
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null) return BadRequest();

        return new JsonResult(await userManager.Users.Where(u =>
            u.EventStatus.Active && u.EventStatus.LocationId == user.EventPlace.LocationId).OrderBy(u => u.EventStatus.Time).Select(u => new CustomOfferUserDisplay()
        {
            DisplayName = u.Name ?? "@" + u.UserName,
            EventDate = u.EventStatus.Time.Value,
            Id = u.Id,
            PfpUrl = u.HasPfp ? pfpService.GetDownloadUrl(u.UserName) : pfpService.GetFallbackUrl(),
            BirthDate = u.BirthDate,
            Gender = u.Gender
        }).ToListAsync());
    }
    
    [BindProperty]
    public CreateOfferModel Input { get; set; }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null) return BadRequest();

        var targetUsers = JsonSerializer.Deserialize<List<string>>(Input.TargetUsersJson);

        var offer = new CustomOffer
        {
            Name = Input.Name,
            HasImage = Input.Picture != null,
            Description = Input.Description,
            DestinedTo = targetUsers,
            ValidUntil = Input.ValidUntil,
            EventPlaceId = user.EventPlace.Id
        };

        dbContext.CustomOffers.Add(offer);

        await dbContext.SaveChangesAsync();

        if (Input.Picture != null)
        {
            await offerPictureService.UploadPicture(Input.Picture.OpenReadStream(), offer.Id, user.EventPlace.Name);
        }

        return RedirectToPage("/Affiliate/CustomOffers/Index");
    }
}