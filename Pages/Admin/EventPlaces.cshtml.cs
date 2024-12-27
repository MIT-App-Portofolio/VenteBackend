using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

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
    }
}
