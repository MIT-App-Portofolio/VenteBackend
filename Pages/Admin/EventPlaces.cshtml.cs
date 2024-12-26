using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Pages.Admin
{
    public class EventPlacesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EventPlacesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [FromQuery]
        public int PageId { get; set; }

        public List<EventPlace> EventPlaces { get; set; }

        public async Task OnGet()
        {
            EventPlaces = await _context.Places.ToListAsync();
        }

        [BindProperty]
        public EventPlaceModel Input { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var place = new EventPlace
            {
                Name = Input.Name,
                Description = Input.Description,
                Location = Input.Location,
                PriceRangeBegin = Input.PriceRangeStart,
                PriceRangeEnd = Input.PriceRangeEnd,
                Images = Input.Images
            };

            _context.Places.Add(place);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
