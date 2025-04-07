using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Server.Data;
using Server.Services;

namespace Server.Pages.Admin.Affiliates;

public class ProgrammaticCreateModel
{
    public List<ProgrammaticCreateEventPlace> Places { get; set; }
}

public class ProgrammaticCreateEventPlace
{
    public string Name { get; set; }
    public string? Description { get; set; }
    
    public int PriceRangeBegin { get; set; }
    public int PriceRangeEnd { get; set; }
    
    public int? AgeRequirement { get; set; }
    
    public string? GoogleMapsLink { get; set; }
    
    public List<string> PictureUrls { get; set; }
    public List<ProgrammaticCreateEventPlaceEvent> Events { get; set; }
}

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
    public string LocationId { get; set; }
}

public class ProgrammaticCreate(ApplicationDbContext dbContext, IEventPlacePictureService pictureService, ILogger<ProgrammaticCreate> logger, UserManager<ApplicationUser> userManager) : PageModel
{
    public void OnGet()
    {
        
    }
   
    [BindProperty]
    public ProgrammaticCreateFormModel Input { get; set; }

    public IActionResult OnPost()
    {
        var model = JsonConvert.DeserializeObject<ProgrammaticCreateModel>(Input.Json);
        if (model == null)
            return BadRequest();
        
        _ = Task.Run(async () => { await Create(model, Input.LocationId); });
        return Page();
    }

    public async Task Create(ProgrammaticCreateModel model, string locationId)
    {
        logger.LogInformation("Began creating places programmatically.");
        using var client = new HttpClient();
        foreach (var place in model.Places)
        {
            logger.LogInformation("Creating for {}", place.Name);
            var user = new ApplicationUser
            {
                UserName = "Programmatic_" + place.Name.Replace(" ", ""),
                Gender = Gender.Male,
                BirthDate = DateTime.MinValue,
                Blocked = [],
                Email = place.Name.Replace(" ", "") + "@" + locationId + ".com",
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await userManager.CreateAsync(user, "Password123+");

            if (!result.Succeeded)
            {
                logger.LogError("Could not create user for {}: {}", place.Name, result.Errors.ToString());
                continue;
            }

            if (!(await userManager.AddToRoleAsync(user, "Affiliate")).Succeeded)
            {
                logger.LogError("Could not add user to affiliate role for {}", place.Name);
                continue;
            }

            var eventPlace = new EventPlace
            {
                Owner = user,
                OwnerId = user.Id,
                Name = place.Name,
                LocationId = locationId,
                Description = place.Description,
                Images = place.PictureUrls.Select((_, i) => "programmatic_" + i).ToList(),
                PriceRangeBegin = place.PriceRangeBegin,
                PriceRangeEnd = place.PriceRangeEnd,
                AgeRequirement = place.AgeRequirement,
                GoogleMapsLink = place.GoogleMapsLink,
                Events = []
            };

            for (var i = 0; i < place.Events.Count; i++)
            {
                var e = place.Events[i];

                if (e.PictureUrl != null)
                {
                    var memoryStream = new MemoryStream();
                    await using (var stream = await client.GetStreamAsync(e.PictureUrl))
                    {
                        await stream.CopyToAsync(memoryStream);
                    }
                    memoryStream.Position = 0;
                    await pictureService.UploadEventPictureAsync(eventPlace, i, memoryStream, "programmatic");
                    await memoryStream.DisposeAsync();
                }
                
                eventPlace.Events.Add(new EventPlaceEvent
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

            for (var i = 0; i < place.PictureUrls.Count; i++)
            {
                var pic = place.PictureUrls[i];
                
                var memoryStream = new MemoryStream();
                await using (var stream = await client.GetStreamAsync(pic))
                {
                    await stream.CopyToAsync(memoryStream);
                }
                memoryStream.Position = 0;
                await pictureService.UploadAsync(eventPlace, memoryStream, "programmatic_" + i);
                await memoryStream.DisposeAsync();
            }

            await dbContext.Places.AddAsync(eventPlace);
        }

        await dbContext.SaveChangesAsync();
    }
}