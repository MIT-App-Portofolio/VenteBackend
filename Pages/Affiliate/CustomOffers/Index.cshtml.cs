using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Interfaces;

namespace Server.Pages.Affiliate.CustomOffers;

public class CustomOfferDisplay
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    
    public int TargetPeople { get; set; }
    
    public DateTimeOffset ValidUntil { get; set; }
}

public class Index(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ICustomOfferPictureService pictureService) : PageModel
{
    public List<CustomOfferDisplay> Offers { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null) return BadRequest();

        Offers = await dbContext.CustomOffers.Where(o => o.EventPlaceId == user.EventPlace.Id).Select(o =>
            new CustomOfferDisplay
            {
                Id = o.Id,
                Description = o.Description,
                Name = o.Name,
                ImageUrl = o.HasImage ? pictureService.GetUrl(o.Id, user.EventPlace!.Name) : null,
                TargetPeople = o.DestinedTo.Count,
                ValidUntil = o.ValidUntil
            }).ToListAsync();
        
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int offerId)
    {
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null) return BadRequest();

        var offer = await dbContext.CustomOffers.Where(o => o.EventPlaceId == user.EventPlace.Id && o.Id == offerId).FirstOrDefaultAsync();

        if (offer == null) return BadRequest();

        dbContext.CustomOffers.Remove(offer);

        await pictureService.DeletePicture(offer.Id, user.EventPlace.Name);

        await dbContext.SaveChangesAsync();

        return RedirectToPage("/Affiliate/CustomOffers/Index");
    }
}