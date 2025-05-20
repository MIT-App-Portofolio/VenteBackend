using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;

namespace Server.Api;

public class NewsEndpoints
{
    public static void MapNewsEndpoints(WebApplication app)
    {
        app.MapGet("/api/news/get_current", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, int exitId) =>
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            if (user == null) return Results.Unauthorized();
            
            var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == exitId && (e.Members.Contains(user.UserName) || e.Leader == user.UserName));
            if (exit == null) return Results.Unauthorized();

            return Results.Ok();

            return exit.LocationId switch
            {
                "salou" => Results.Ok(new CurrentNewsDto
                {
                    Name = "Entradas tropical 33% de descuento",
                    UniqueId = "tropical-initial-discount",
                    Description = "Las entradas a tropical salen a 10\u20ac en vez de 15\u20ac. Solo en Vente.",
                    Path = $"/places?selectedExitId={exitId}"
                }),
                _ => Results.Ok()
            };
        });
    }
}