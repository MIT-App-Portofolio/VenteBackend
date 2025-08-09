using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Interfaces;

namespace Server.Pages.Affiliate;

public class EditEvent(UserManager<ApplicationUser> userManager, IEventPlacePictureService pictureService) : PageModel
{
    [FromRoute] public int EventId { get; set; }
    
    public class EventModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public DateTime Time { get; set; }
        public string? Description { get; set; }
        public string? PurchaseLink { get; set; }
    }
    
    [BindProperty]
    public EventModel Event { get; set; }
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

        Event = new EventModel
        {
            Name = @event.Name,
            Time = @event.Time.DateTime,
            Description= @event.Description,
            PurchaseLink = @event.PurchaseLink,
        };
        
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .ThenInclude(u => u.Events)
            .ThenInclude(e => e.Offers)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
        
        if (user == null) return NotFound();
        
        var @event = user.EventPlace.Events.FirstOrDefault(e => e.Id == EventId);
        
        if (@event == null) return NotFound();

        if ((@event.Name != Event.Name || @event.Time != Event.Time) && !string.IsNullOrEmpty(@event.Image))
            await pictureService.MoveEventPictureAsync(user.EventPlace, @event.Image, @event.Name, @event.Time, Event.Name, Event.Time);

        @event.Name = Event.Name;
        @event.Description = Event.Description;
        @event.Time = Event.Time;
        @event.PurchaseLink = Event.PurchaseLink;

        await userManager.UpdateAsync(user);
        
        return RedirectToPage("/Affiliate/Event", new {EventId});
    }
}