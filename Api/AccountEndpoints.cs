using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Amazon.Runtime;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server.Config;
using Server.Data;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using Server.Services.Concrete;
using Server.Services.Interfaces;
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
            
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);

            if (!Validator.TryValidateObject(model, context, validationResults, true))
                return Results.BadRequest(validationResults);

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
                CreatedAt = DateTimeOffset.Now,
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
            
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);

            if (!Validator.TryValidateObject(model, context, validationResults, true))
                return Results.BadRequest(validationResults);

            var validatedToken = await validator.ValidateToken(model.Id);
            var email = validatedToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Gender = model.Gender,
                BirthDate = model.BirthDate.HasValue 
                    ? new DateTimeOffset(model.BirthDate.Value) 
                    : (DateTimeOffset?)null,
                CreatedAt = DateTimeOffset.Now,
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

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);

            if (!Validator.TryValidateObject(model, context, validationResults, true))
                return Results.BadRequest(validationResults);
            
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Gender = model.Gender,
                BirthDate = model.BirthDate.HasValue 
                    ? new DateTimeOffset(model.BirthDate.Value) 
                    : (DateTimeOffset?)null,
                CreatedAt = DateTimeOffset.Now,
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
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);

            if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
                return Results.BadRequest(validationResults);
            
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

        app.MapPost("/api/account/set_custom_note", [JwtAuthorize] async (UserManager<ApplicationUser> userManager, HttpContext context, string note) =>
        {
            var user = await userManager.FindByNameAsync(context.User.Identity.Name);
            if (user == null) return Results.BadRequest("User not found.");

            if (note.Length > 50) return Results.BadRequest("Note cannot be longer than 50 chars");

            if (note == "")
            {
                user.CustomNote = null;
                user.NoteWasSet = null;
            }
            else
            {
                user.CustomNote = note;
                user.NoteWasSet = DateTimeOffset.Now;
            }

            await userManager.UpdateAsync(user);

            return Results.Ok();
        });
        
        app.MapPost("/api/account/remove_custom_note", [JwtAuthorize] async (UserManager<ApplicationUser> userManager, HttpContext context) =>
        {
            var user = await userManager.FindByNameAsync(context.User.Identity.Name);
            if (user == null) return Results.BadRequest("User not found.");

            user.CustomNote = null;
            user.NoteWasSet = null;

            await userManager.UpdateAsync(user);

            return Results.Ok();
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
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(model);

                if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
                    return Results.BadRequest(validationResults);
            
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

        app.MapGet("/api/account/get_offers", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, HttpContext context,
                ICustomOfferPictureService offerPictureService, IEventPlacePictureService eventPlacePictureService) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var offers = dbContext.CustomOffers
                    .Where(o => o.DestinedTo.Contains(user.Id))
                    .ToList();

                var cutoffTime = user.EventStatus.Time ?? DateTimeOffset.UtcNow;

                var placeIds = offers.Select(o => o.EventPlaceId).Distinct();
                var places = (await dbContext.Places
                    .Include(p => p.Events)
                    .ThenInclude(e => e.Offers)
                    .Where(p => placeIds.Contains(p.Id))
                    .Select(p =>
                        new
                        {
                            Place = p,
                            Events = p.Events.OrderBy(o => o.Time)
                                .Where(o => (o.Time - cutoffTime).Days < 14).ToList()
                        }
                    )
                    .ToListAsync()).Select(place =>
                {
                    place.Place.Events = place.Events.Select((e, i) =>
                    {
                        var index = place.Place.Events.IndexOf(e);
                        e.Image = e.Image == null
                            ? null
                            : eventPlacePictureService.GetEventPictureUrl(place.Place, index);
                        return e;
                    }).ToList();
                    return (place.Place.Id, new EventPlaceDto(place.Place, eventPlacePictureService.GetDownloadUrls(place.Place)));
                }).ToDictionary();

                var dtos = offers.Select(o =>
                {
                    var place = places[o.EventPlaceId];
                    var imageUrl = o.HasImage ? offerPictureService.GetUrl(o.Id, place.Name) : null;
                    return new CustomOfferDto(o, imageUrl, place);
                });

                return Results.Ok(dtos);
            });

        app.MapGet("/api/account/get_offer_qr", [JwtAuthorize]
            async (HttpContext context, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
                ICustomOfferPictureService customOfferPictureService, CustomOfferTokenStorage tokenStorage, int offerId) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var offer = await dbContext.CustomOffers.FirstOrDefaultAsync(o =>
                o.Id == offerId && o.DestinedTo.Contains(user.Id));

            if (offer == null) return Results.BadRequest();

            return Results.Ok(tokenStorage.Add(offer.Id, user.Id));
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
            async (UserManager<ApplicationUser> userManager, HttpContext context, ApplicationDbContext dbContext, IProfilePictureService pfpService) =>
            {
                var user = await userManager.Users.Include(u => u.Notifications).FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                if (user.HasPfp)
                    await pfpService.RemoveProfilePictureAsync(user.UserName);

                await userManager.DeleteAsync(user);

                return Results.Ok();
            });
    }
}