using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Data;
using Server.Services.Interfaces;

namespace Server.Pages.Admin.EventPlaces;

public class ProgrammaticCreateEventPlaceEvent
{
    public DateTimeOffset Time { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? PictureUrl { get; set; }
    public List<ProgrammaticCreateEventOffer> Offers { get; set; }
}

public class ProgrammaticCreateEventOffer
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Price { get; set; }
}

public class ProgrammaticCreateFormModel
{
    public string Json { get; set; }
}

public class ProgrammaticCreate(ApplicationDbContext dbContext, IEventPlacePictureService pictureService, ILogger<ProgrammaticCreate> logger, UserManager<ApplicationUser> userManager) : PageModel
{
    public void OnGet()
    {
        
    }
   
    [BindProperty]
    public ProgrammaticCreateFormModel Input { get; set; }

    public IActionResult OnPost([FromQuery] int placeId)
    {
        var model = JsonConvert.DeserializeObject<List<ProgrammaticCreateEventPlaceEvent>>(Input.Json);
        if (model == null)
            return BadRequest();
        
        _ = Task.Run(async () => { await Create(model, placeId); });
        return Page();
    }

    public async Task Create(List<ProgrammaticCreateEventPlaceEvent> model, int placeId)
    {
        logger.LogInformation("Began creating places programmatically.");
        using var client = new HttpClient();
        var place = await dbContext.Places.FirstOrDefaultAsync(p => p.Id == placeId);
        if (place == null)
        {
            logger.LogError("Could not find event place with id {}", place.Id);
        }

        for (var i = 0; i < model.Count; i++)
        {
            var e = model[i];

            if (e.PictureUrl != null)
            {
                var memoryStream = new MemoryStream();
                await using (var stream = await client.GetStreamAsync(e.PictureUrl))
                {
                    await stream.CopyToAsync(memoryStream);
                }

                memoryStream.Position = 0;
                await pictureService.UploadEventPictureAsync(place, i, memoryStream, "programmatic");
                await memoryStream.DisposeAsync();
            }

            place.Events.Add(new EventPlaceEvent
            {
                Name = e.Name,
                Description = e.Description,
                Time = e.Time,
                Image = e.PictureUrl == null ? null : "programmatic",
                Offers = e.Offers.Select(o => new EventPlaceOffer
                {
                    Name = o.Name,
                    Description = o.Description,
                    Price = o.Price
                }).ToList()
            });
        }

        await dbContext.SaveChangesAsync();
    }
}