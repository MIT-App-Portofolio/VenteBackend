using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Interfaces;

namespace Server.Api;

public static class AlbumEndpoints
{
    public static void MapAlbumEndpoints(WebApplication app)
    {
        app.MapGet("/api/album/get_own_pictures", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            if (!user.EventStatus.Active) return Results.BadRequest();

            if (user.EventStatus.EventGroupId == null) return Results.Ok();

            var group = await dbContext.Groups.Where(g => g.Id == user.EventStatus.EventGroupId).FirstAsync();

            if (group.SharedAlbumId == null) return Results.Ok();

            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Id == group.SharedAlbumId)
                .FirstAsync();

            return Results.Ok(new OwnPicturesDto
            {
                AlbumId = album.Id,
                Pictures = album.Pictures.Where(p => p.Uploader == user.UserName).Select(p => new AlbumPictureDto(p)).ToList()
            });
        });

        app.MapGet("/api/album/access_picture/{albumId}/{pictureId}", [JwtAuthorize] async (HttpContext context,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IAlbumPictureService pictureService, int albumId, int pictureId) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .FirstOrDefaultAsync(a => a.Id == albumId && a.Members.Contains(user.Id));

            if (album == null) return Results.BadRequest();

            return album.Pictures.Any(p => p.Id == pictureId) ? Results.Stream(await pictureService.GetStream(albumId, pictureId)) : Results.NotFound();
        });

        app.MapPost("/api/album/upload_picture", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IAlbumPictureService pictureService, IFormFile file) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            if (!user.EventStatus.Active) return Results.BadRequest();
            
            if (user.EventStatus.EventGroupId == null)
            {
                var newGroup = new EventGroup
                {
                    Members = [user.Id],
                    AwaitingInvite = []
                };

                dbContext.Groups.Add(newGroup);
                await dbContext.SaveChangesAsync();

                user.EventStatus.EventGroupId = newGroup.Id;
                await userManager.UpdateAsync(user);
            }
            
            var group = await dbContext.Groups.Where(g => g.Id == user.EventStatus.EventGroupId).FirstAsync();

            if (group.SharedAlbumId == null)
            {
                var newAlbum = new SharedAlbum
                {
                    Members = [..group.Members],
                    EventDate = user.EventStatus.Time.Value,
                    PlaceId = user.EventStatus.LocationId,
                    AvailableAt = DateTimeOffset.Now.AddHours(6),
                    DeletionDate = DateTimeOffset.Now.AddDays(14),
                };

                dbContext.Albums.Add(newAlbum);
                await dbContext.SaveChangesAsync();

                group.SharedAlbumId = newAlbum.Id;
                dbContext.Groups.Update(group);
                await dbContext.SaveChangesAsync();
            }

            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Id == group.SharedAlbumId)
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

        app.MapPost("/api/album/delete_picture", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IAlbumPictureService pictureService, int id) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            if (!user.EventStatus.Active) return Results.BadRequest();

            if (user.EventStatus.EventGroupId == null) return Results.BadRequest();

            var group = await dbContext.Groups.Where(g => g.Id == user.EventStatus.EventGroupId).FirstAsync();

            if (group.SharedAlbumId == null) return Results.BadRequest();
            
            var album = await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Id == group.SharedAlbumId)
                .FirstAsync();

            if (album.Pictures.RemoveAll(a => a.Uploader == user.UserName && a.Id == id) == 0) return Results.Ok();
            
            await dbContext.SaveChangesAsync();

            await pictureService.RemoveAlbumPicture(album.Id, id);

            return Results.Ok();
        });
        
        app.MapGet("/api/album/get_albums", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            return Results.Ok(await dbContext.Albums
                .Include(a => a.Pictures)
                .Where(a => a.Members.Contains(user.Id) /*&& a.AvailableAt >= DateTimeOffset.Now*/ && a.Pictures.Count > 0)
                .Select(a => new SharedAlbumDto(a))
                .ToListAsync());
        });
    }
}