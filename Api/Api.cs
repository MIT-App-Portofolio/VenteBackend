using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services;
using Server.Services.Interfaces;

namespace Server.Api;

public class JwtAuthorizeAttribute : AuthorizeAttribute
{
    public JwtAuthorizeAttribute()
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}

public static class Api
{

    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/get_locations",
            async (ApplicationDbContext dbContext, ILocationImageService imageService) =>
            {
                return await dbContext.Locations.Select(l => new LocationDto(l, imageService.GetUrl(l.Id))).ToListAsync();
            });
        
        AccountEndpoints.MapAccountEndpoints(app);
        SafetyEndpoints.MapSafetyEndpoints(app);
        AccountAccessEndpoints.MapAccountAccessEndpoints(app);
        ExitAlbumEndpoints.MapExitAlbumEndpoints(app);
        EventEndpoints.MapEventEndpoints(app);
        VenueEndpoints.MapVenueEndpoints(app);
        AlbumEndpoints.MapAlbumEndpoints(app);
        ExitEndpoints.MapExitEndpoints(app);
        MessageEndpoints.MapMessageEndpoints(app);
        LikeEndpoints.MapLikeEndpoints(app);
        UserSearchEndpoints.MapUserSearchEndpoints(app);
        NotificationEndpoints.MapNotificationEndpoints(app);
        FollowEndpoints.MapFollowEndpoints(app);
        StatusEndpoints.MapStatusEndpoints(app);
        NewsEndpoints.MapNewsEndpoints(app);
    }
}