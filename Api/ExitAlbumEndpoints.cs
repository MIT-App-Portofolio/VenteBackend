using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Interfaces;

namespace Server.Api;

public static class ExitAlbumEndpoints
{
    public static void MapExitAlbumEndpoints(WebApplication app)
    {
        app.MapGet("/api/exit_album/allowed", [JwtAuthorize] async (HttpContext context,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var exit = await GetActiveExit(dbContext, user);

            return Results.Ok(exit != null);
        });

        app.MapGet("/api/exit_album/get_own_pictures", [JwtAuthorize] async (HttpContext context,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var exit = await GetActiveExit(dbContext, user);

            if (exit == null) return Results.BadRequest();

            if (exit.AlbumId == null) return Results.Ok();

            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Id == exit.AlbumId)
                .FirstAsync();

            return Results.Ok(new OwnPicturesDto
            {
                AlbumId = album.Id,
                Pictures = album.Pictures.Where(p => p.Uploader == user.UserName).Select(p => new AlbumPictureDto(p))
                    .ToList()
            });
        });

        app.MapGet("/api/exit_album/access_picture/{albumId}/{pictureId}", [JwtAuthorize] async (HttpContext context,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IAlbumPictureService pictureService, int albumId, int pictureId) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .FirstOrDefaultAsync(a => a.Id == albumId && a.Members.Contains(user.Id));

            if (album == null) return Results.BadRequest();

            return album.Pictures.Any(p => p.Id == pictureId) ? Results.Stream(await pictureService.GetStream(albumId, pictureId)) : Results.NotFound();
        });

        app.MapPost("/api/exit_album/upload_picture", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IAlbumPictureService pictureService, IFormFile file) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();
            
            var exit = await GetActiveExit(dbContext, user);

            if (exit == null) return Results.BadRequest();

            if (exit.AlbumId == null) return Results.Ok();

            if (exit.AlbumId == null)
            {
                List<string> members = [..exit.Members, exit.Leader];
                var newAlbum = new SharedAlbum
                {
                    Members = await userManager.Users.Where(u => members.Contains(u.UserName)).Select(u => u.Id).ToListAsync(),
                    EventDate = user.EventStatus.Time.Value,
                    PlaceId = user.EventStatus.LocationId,
                    AvailableAt = DateTimeOffset.Now.AddHours(6),
                    DeletionDate = DateTimeOffset.Now.AddDays(14),
                };

                dbContext.Albums.Add(newAlbum);
                await dbContext.SaveChangesAsync();

                exit.AlbumId = newAlbum.Id;
                dbContext.Exits.Update(exit);
                await dbContext.SaveChangesAsync();
            }

            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Id == exit.AlbumId)
                .FirstAsync();
            
            var pic = new AlbumPicture
            {
                Uploader = user.UserName,
                Time = DateTimeOffset.Now
            };
            
            album.Pictures.Add(pic);

            dbContext.Albums.Update(album);
            await dbContext.SaveChangesAsync();

            await pictureService.UploadAlbumPicture(file.OpenReadStream(), album.Id, pic.Id);

            return Results.Ok(album.Id);
        }).DisableAntiforgery();

        app.MapPost("/api/exit_album/delete_picture", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IAlbumPictureService pictureService, int id) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();
            
            var exit = await GetActiveExit(dbContext, user);

            if (exit == null) return Results.BadRequest();

            if (exit.AlbumId == null) return Results.BadRequest();
            
            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Id == exit.AlbumId)
                .FirstAsync();

            if (album.Pictures.RemoveAll(a => a.Uploader == user.UserName && a.Id == id) == 0) return Results.Ok();
            
            await dbContext.SaveChangesAsync();

            await pictureService.RemoveAlbumPicture(album.Id, id);

            return Results.Ok();
        });
        
        app.MapGet("/api/exit_album/get_albums", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            return Results.Ok(await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Members.Contains(user.Id) /*&& a.AvailableAt >= DateTimeOffset.Now*/ && a.Pictures.Count > 0)
                .Select(a => new SharedAlbumDto(a))
                .ToListAsync());
        });
    }

    private static async Task<ExitInstance?> GetActiveExit(ApplicationDbContext dbContext, ApplicationUser user)
    {
        return await dbContext.Exits.FirstOrDefaultAsync(e =>
            (e.Members.Contains(user.UserName) || e.Leader == user.UserName) &&
            e.Dates.Any(d => d.Date == DateTimeOffset.UtcNow.Date));
    }
}