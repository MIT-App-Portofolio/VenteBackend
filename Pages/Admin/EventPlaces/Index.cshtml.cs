using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin.EventPlaces
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
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
