using System.Diagnostics;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server.Config;
using Server.Data;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using SixLabors.ImageSharp;

namespace Server.Api;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(WebApplication app)
    {
        app.MapGet("/api/account/google_should_register", async (UserManager<ApplicationUser> userManager,
            IOptions<GoogleConfig> config, string id) =>
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(id, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = config.Value.ClientIds
            });

            return Results.Ok(await userManager.FindByEmailAsync(payload.Email) == null);
        });

        app.MapPost("/api/account/login_google", async (UserManager<ApplicationUser> userManager,
            JwtTokenManager tokenManager, IOptions<GoogleConfig> config, string id) =>
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(id,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = config.Value.ClientIds
                });

            var user = await userManager.FindByEmailAsync(payload.Email);
            if (user == null) return Results.BadRequest("User not found.");

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });

        app.MapPost("/api/account/register_google", async (UserManager<ApplicationUser> userManager,
            JwtTokenManager tokenManager, IOptions<GoogleConfig> config, GoogleRegister model) =>
        {
            if (model.UserName == "fallback")
                return Results.BadRequest("Fallback username is reserved.");

            if (model.BirthDate > DateTime.Today.AddYears(-16))
                return Results.BadRequest("User must be at least 16 years old.");

            var payload = await GoogleJsonWebSignature.ValidateAsync(model.Id,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = config.Value.ClientIds
                });

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Gender = model.Gender,
                BirthDate = model.BirthDate.HasValue 
                    ? new DateTimeOffset(model.BirthDate.Value) 
                    : (DateTimeOffset?)null,
                Blocked = [],
                Email = payload.Email,
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await userManager.CreateAsync(user);

            if (!result.Succeeded) return Results.BadRequest(result.Errors);

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });
        
        app.MapGet("/api/account/apple_should_register", async (UserManager<ApplicationUser> userManager,
            AppleTokenValidatorService validator , string id) =>
        {
            var validatedToken = await validator.ValidateToken(id);
            var email = validatedToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            return Results.Ok(await userManager.FindByEmailAsync(email) == null);
        });

        app.MapPost("/api/account/login_apple", async (UserManager<ApplicationUser> userManager,
            JwtTokenManager tokenManager, AppleTokenValidatorService validator, string id) =>
        {
            var validatedToken = await validator.ValidateToken(id);
            var email = validatedToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return Results.BadRequest("User not found.");

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });

        app.MapPost("/api/account/register_apple", async (UserManager<ApplicationUser> userManager,
            JwtTokenManager tokenManager, AppleTokenValidatorService validator, AppleRegister model) =>
        {
            if (model.UserName == "fallback")
                return Results.BadRequest("Fallback username is reserved.");

            if (model.BirthDate > DateTime.Today.AddYears(-16))
                return Results.BadRequest("User must be at least 16 years old.");

            var validatedToken = await validator.ValidateToken(model.Id);
            var email = validatedToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Gender = model.Gender,
                BirthDate = model.BirthDate.HasValue 
                    ? new DateTimeOffset(model.BirthDate.Value) 
                    : (DateTimeOffset?)null,
                Blocked = [],
                Email = email,
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded) return Results.BadRequest(result.Errors);

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });
        
        app.MapPost("/api/account/register", async (UserManager<ApplicationUser> userManager,
            JwtTokenManager tokenManager, RegisterModel model) =>
        {
            if (model.UserName == "fallback")
                return Results.BadRequest("Fallback username is reserved.");

            if (model.BirthDate > DateTime.Today.AddYears(-16))
                return Results.BadRequest("User must be at least 16 years old.");

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Gender = model.Gender,
                BirthDate = model.BirthDate.HasValue 
                    ? new DateTimeOffset(model.BirthDate.Value) 
                    : (DateTimeOffset?)null,
                Blocked = [],
                Email = model.Email,
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) return Results.BadRequest(result.Errors);

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });

        app.MapPost("/api/account/login", async (UserManager<ApplicationUser> userManager, JwtTokenManager tokenManager,
            ILogger<Program> logger, LoginModel model) =>
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return Results.BadRequest("Invalid login attempt.");

            var success = await userManager.CheckPasswordAsync(user, model.Password);
            
            if (!success) return Results.BadRequest("Invalid login attempt.");
            if (model.Email == "appletesting@example.com")
            {
                logger.LogInformation("Apple devs attempted to login with password " + model.Password + " success: " + success);
            }

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });

        app.MapPost("/api/account/set_notification_key", [JwtAuthorize] async (UserManager<ApplicationUser> userManager, HttpContext context, string key) =>
        {
            var user = await userManager.FindByNameAsync(context.User.Identity.Name);
            if (user == null) return Results.BadRequest("User not found.");

            user.NotificationKey = key;
            await userManager.UpdateAsync(user);

            return Results.Ok();
        });

        app.MapPost("/api/account/update_profile",
            [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ProfileModel model) =>
            {
                var user = await userManager.FindByNameAsync(context.User.Identity.Name);
                if (user == null) return Results.BadRequest("User not found.");

                user.Name = model.Name;
                user.IgHandle = model.IgHandle;
                user.Description = model.Description;

                await userManager.UpdateAsync(user);

                return Results.Ok();
            });

        app.MapGet("/api/account/info", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                List<string>? withUsernames = null;

                if (user.EventStatus.EventGroupId != null)
                {
                    var group = await dbContext.Groups.FindAsync(user.EventStatus.EventGroupId);
                    if (group == null) throw new UnreachableException("Group could not be found.");

                    withUsernames = await userManager.Users.Where(u => group.Members.Contains(u.Id))
                        .Select(u => u.UserName!).ToListAsync();
                }

                return Results.Ok(new UserDto(user, withUsernames));
            });

        app.MapPost("/api/account/update_pfp", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
                IFormFile file,
                HttpContext context, IProfilePictureService pfpService) =>
            {
                if (!file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Only JPEG files are allowed.");

                if (file.Length > 3 * 1024 * 1024)
                    return Results.BadRequest("File size exceeds the 3MB limit.");

                await using (var stream = file.OpenReadStream())
                {
                    using (var image = await Image.LoadAsync(stream))
                        if (Math.Abs(image.Height - image.Width) > 5)
                            return Results.BadRequest("Image must have a 1:1 aspect ratio.");
                }

                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                user.HasPfp = true;
                user.PfpVersion += 1;
                await userManager.UpdateAsync(user);

                await pfpService.UploadProfilePictureAsync(file.OpenReadStream(), context.User.Identity.Name);
                return Results.Ok();
            })
            .DisableAntiforgery();

        app.MapPost("/api/account/remove_pfp", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
            IProfilePictureService pfpService, HttpContext context) =>
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (!user.HasPfp) return Results.Ok();

            await pfpService.RemoveProfilePictureAsync(context.User.Identity.Name);

            user.HasPfp = false;
            await userManager.UpdateAsync(user);

            return Results.Ok();
        });

        app.MapPost("/api/account/delete", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, HttpContext context) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                await userManager.DeleteAsync(user);

                return Results.Ok();
            });
    }
}