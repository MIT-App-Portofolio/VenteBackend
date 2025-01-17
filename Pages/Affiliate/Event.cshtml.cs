using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Affiliate;

public class Event(UserManager<ApplicationUser> userManager) : PageModel
{
    [FromRoute] public int EventId { get; set; }

    public class EventModel
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public string? Description { get; set; }
    }

    public class OfferModel
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public int? Price { get; set; }
    }
    
    public EventModel EventInfo { get; set; }
    public List<(OfferModel, int)> Offers { get; set; }
    
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .ThenInclude(u => u.Events)
            .ThenInclude(e => e.Offers)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
        
        if (user == null) return NotFound();
        
        var @event = user.EventPlace.Events.FirstOrDefault(e => e.Id == EventId);
        
        if (@event == null) return NotFound();

        EventInfo = new EventModel
        {
            Name = @event.Name,
            Time = @event.Time.DateTime,
            Description= @event.Description,
        };
        
        Offers = @event.Offers.Select(o => (new OfferModel
        {
            Name = o.Name,
            Description = o.Description,
            Price = o.Price
        }, o.Id)).ToList();
        
        return Page();
    }
    
    public class CreateOfferModel
    {
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public int? Price { get; set; }
    }

    [BindProperty]
    public CreateOfferModel NewOffer { get; set; }

    public async Task<IActionResult> OnPostCreateOfferAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .ThenInclude(p => p.Events)
            .ThenInclude(e => e.Offers)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null) return NotFound();

        var @event = user.EventPlace.Events.FirstOrDefault(e => e.Id == EventId);
        
        if (@event == null) return NotFound();

        @event.Offers.Add(new EventPlaceOffer
        {
            Name = NewOffer.Name,
            Description = NewOffer.Description,
            Price = NewOffer.Price
        });

        await userManager.UpdateAsync(user);

        return RedirectToPage("/Affiliate/Event", new { EventId });
    }
    
    public async Task<IActionResult> OnPostDeleteOfferAsync(int offerId)
    {
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .ThenInclude(p => p.Events)
            .ThenInclude(e => e.Offers)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null) return NotFound();
        
        var @event = user.EventPlace.Events.FirstOrDefault(e => e.Id == EventId);
        
        if (@event == null) return NotFound();
        
        var offer = @event.Offers.FirstOrDefault(o => o.Id == offerId);
        
        if (offer == null) return NotFound();

        @event.Offers.Remove(offer);

        await userManager.UpdateAsync(user);

        return RedirectToPage("/Affiliate/Event", new { EventId });
    }
}