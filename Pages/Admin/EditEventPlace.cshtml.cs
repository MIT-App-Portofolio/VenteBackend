using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Server.Data;
using Server.Models;

namespace Server.Pages.Admin
{
    public class EditEventPlaceModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditEventPlaceModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public EventPlaceModel EventPlace { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var eventPlace = await _context.Places.FindAsync(Id);

            if (eventPlace == null)
                return NotFound();

            EventPlace = new EventPlaceModel(eventPlace);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine(ModelState.IsValid);
            if (!ModelState.IsValid)
                return Page();

            var existingEventPlace = await _context.Places.FindAsync(Id);
            Console.WriteLine(existingEventPlace);
            if (existingEventPlace == null)
                return NotFound();

            Console.WriteLine(EventPlace.Location);

            existingEventPlace.Name = EventPlace.Name;
            existingEventPlace.Description = EventPlace.Description;
            existingEventPlace.Location = EventPlace.Location;
            existingEventPlace.PriceRangeBegin = EventPlace.PriceRangeStart;
            existingEventPlace.PriceRangeEnd = EventPlace.PriceRangeEnd;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/EventPlaces", new { PageId = 0 });
        }
    }
}
