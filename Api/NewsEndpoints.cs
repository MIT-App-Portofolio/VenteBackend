using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;

namespace Server.Api;

public class NewsEndpoints
{
    public static void MapNewsEndpoints(WebApplication app)
    {
        app.MapGet("/api/news/get_current", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, int exitId, bool? testing) =>
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            if (user == null) return Results.Unauthorized();
            
            var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == exitId && (e.Members.Contains(user.UserName) || e.Leader == user.UserName));
            if (exit == null) return Results.Unauthorized();

            if (testing == null || !testing.Value)
                return Results.Ok();

            return exit.LocationId switch
            {
                "salou" => Results.Ok(new CurrentNewsDto
                {
                    Name = "La mejor discoteca de Salou",
                    UniqueId = "tropical-initial-discount-2",
                    Description = "Las entradas para Tropical Salou están disponibles desde la app. Comprad las entradas ya a la mejor discoteca de salou y disfrutad de Tropical.",
                    Path = $"/places?selectedExitId={exitId}"
                }),
                _ => Results.Ok()
            };
        });
    }
}