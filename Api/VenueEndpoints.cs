using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Concrete;
using Server.Services.Interfaces;

namespace Server.Api;

public class VenueEndpoints
{
    public static void MapVenueEndpoints(WebApplication app)
    {
        app.MapGet("/api/venue/is_affiliate", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.BadRequest();

            return Results.Ok(user.EventPlace != null);
        });
        
        app.MapGet("/api/venue/scan_offer_qr", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
                CustomOfferTokenStorage tokenStorage, ICustomOfferPictureService pictureService, string token) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null || user.EventPlace == null) return Results.BadRequest();

            var parsedToken = tokenStorage.Access(token);

            if (parsedToken == null)
                return Results.BadRequest("token_not_found");

            var offer = await dbContext.CustomOffers.FirstOrDefaultAsync(o =>
                o.Id == parsedToken.OfferId);

            if (offer == null)
                return Results.BadRequest("unknown_offer");
            
            if (offer.EventPlaceId != user.EventPlace.Id)
                return Results.BadRequest("wrong_venue");

            var dto = new CustomOfferOnlyDto(offer,
                offer.HasImage ? pictureService.GetUrl(offer.Id, user.EventPlace.Name) : null);

            offer.DestinedTo.Remove(user.Id);

            await dbContext.SaveChangesAsync();

            return Results.Ok(dto);
        });
    }
}