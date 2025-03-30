using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;

namespace Server.Api;

public static class SafetyEndpoints
{
    public static void MapSafetyEndpoints(WebApplication app)
    {
        app.MapPost("/api/safety/report", async (string userName, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IProfilePictureService pfpService) =>
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null) return Results.BadRequest("User not found");

            var existingReport = await dbContext.Reports.FirstOrDefaultAsync(r =>
                r.Username == userName && 
                r.HasPfp == user.HasPfp && 
                r.Name == user.Name && 
                r.Description == user.Description &&
                r.PfpVersion == user.PfpVersion && 
                r.Gender == user.Gender &&
                r.IgHandle == user.IgHandle);

            if (existingReport != null)
            {
                existingReport.ReportCount += 1;
                await dbContext.SaveChangesAsync();
                return Results.Ok();
            }

            var report = new Report
            {
                Username = user.UserName,
                Name = user.Name,
                HasPfp = user.HasPfp,
                PfpVersion = user.PfpVersion,
                IgHandle = user.IgHandle,
                ReportCount = 1,
                Description = user.Description,
                Gender = user.Gender
            };

            var pfpAlreadyUploaded = await dbContext.Reports.AnyAsync(r =>
                r.Username == user.UserName && r.HasPfp == user.HasPfp && r.PfpVersion == user.PfpVersion);

            await dbContext.Reports.AddAsync(report);
            await dbContext.SaveChangesAsync();

            if (!user.HasPfp || pfpAlreadyUploaded)
            {
                return Results.Ok();
            }
            
            using var httpClient = new HttpClient();
                
            var memoryStream = new MemoryStream();
            await using (var stream = await httpClient.GetStreamAsync(pfpService.GetDownloadUrl(user.UserName)))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;
            await pfpService.UploadReportPictureAsync(memoryStream, user.UserName, user.PfpVersion);
            await memoryStream.DisposeAsync();
            
            return Results.Ok();
        });
    }
}