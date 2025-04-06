using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;

namespace Server.Pages.Admin;

public class CreateLocationModel
{
    [Required]
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    
    [Required]
    public double Latitude { get; set; }
    [Required]
    public double Longitude { get; set; }
    
    [Required]
    public IFormFile Picture { get; set; }
}

public class Locations(ApplicationDbContext dbContext, ILocationImageService locationImageService) : PageModel
{
    public List<LocationInfo> AllLocations { get; set; }
    public async Task OnGetAsync()
    {
        AllLocations = await dbContext.Locations.ToListAsync();
    }
    
    [BindProperty]
    public CreateLocationModel Input { get; set; }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await dbContext.Locations.AddAsync(new LocationInfo()
        {
            Id = Input.Id,
            Name = Input.Name,
            Latitude = Input.Latitude,
            Longitude = Input.Longitude
        });

        await dbContext.SaveChangesAsync();

        await locationImageService.UploadLocation(Input.Picture.OpenReadStream(), Input.Id);

        return RedirectToPage("/Admin/Locations");
    }

    public async Task<IActionResult> OnPostDeleteAsync(string locId)
    {
        var loc = await dbContext.Locations.FirstOrDefaultAsync(l => l.Id == locId);
        if (loc == null) return NotFound();
        dbContext.Locations.Remove(loc);
        await dbContext.SaveChangesAsync();
        await locationImageService.RemoveLocation(locId);
        return RedirectToPage("/Admin/Locations");
    }
}