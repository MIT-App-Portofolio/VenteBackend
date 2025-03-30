using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Server.Data;
using Server.Models.Dto;

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
            () => { return Enum.GetValues<Location>().Select(location => new LocationDto(location)).ToList(); });
        
        AccountEndpoints.MapAccountEndpoints(app);
        SafetyEndpoints.MapSafetyEndpoints(app);
        AccountAccessEndpoints.MapAccountAccessEndpoints(app);
        EventEndpoints.MapEventEndpoints(app);
    }
}