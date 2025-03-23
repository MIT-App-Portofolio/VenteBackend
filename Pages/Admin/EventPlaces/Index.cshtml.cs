using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin.EventPlaces
{
    public class IndexModel(ApplicationDbContext context) : PageModel
    {
        public List<EventPlace> EventPlaces { get; set; }

        public async Task OnGet()
        {
            EventPlaces = await context.Places.ToListAsync();
        }
    }
}
