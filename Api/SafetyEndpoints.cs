using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;

namespace Server.Api;

public static class SafetyEndpoints
{
    public static void MapSafetyEndpoints(WebApplication app)
    {
        app.MapPost("/api/safety/report", async (ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IProfilePictureService pfpService, ILogger<Program> logger, string username) =>
        {
            if (app.Environment.IsEnvironment("Sandbox"))
            {
                logger.LogInformation("Reported " + username + " in sandbox mode.");
                return Results.Ok();
            }
            
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null) return Results.BadRequest("User not found");

            var existingReport = await dbContext.Reports.FirstOrDefaultAsync(r =>
                r.Username == username && 
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

        app.MapPost("/api/safety/block", [JwtAuthorize] async (UserManager<ApplicationUser> userManager, HttpContext context, string username) =>
        {
            if (!app.Environment.IsEnvironment("Sandbox") && !await userManager.Users.AnyAsync(u => u.UserName == username)) return Results.BadRequest();
            
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user.Blocked == null)
            {
                user.Blocked = [username];
            }
            else
            {
                if (!user.Blocked.Contains(username))
                    user.Blocked.Add(username);
            }
            
            await userManager.UpdateAsync(user);
            
            return Results.Ok();
        });
    }
}