using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;
using Server.Models;

namespace Server.Pages.Admin.EventPlaces
{
    public class EditModel(ApplicationDbContext context) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public EventPlaceModel EventPlace { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var eventPlace = await context.Places.FindAsync(Id);

            if (eventPlace == null)
                return NotFound();

            EventPlace = new EventPlaceModel(eventPlace);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var existingEventPlace = await context.Places.FindAsync(Id);
            if (existingEventPlace == null)
                return NotFound();

            existingEventPlace.Name = EventPlace.Name;
            existingEventPlace.Description = EventPlace.Description;
            existingEventPlace.LocationId = EventPlace.LocationId;
            existingEventPlace.PriceRangeBegin = EventPlace.PriceRangeStart;
            existingEventPlace.PriceRangeEnd = EventPlace.PriceRangeEnd;

            await context.SaveChangesAsync();

            return RedirectToPage("/Admin/EventPlaces/Index", new { PageId = 0 });
        }
    }
}
