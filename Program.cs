using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.SharedInterfaces;
using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server.Config;
using Server.Models;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddAuthentication().AddCookie(options =>
{
    options.LoginPath = "/api/account/login";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

builder.Services.Configure<AwsConfig>(builder.Configuration.GetSection("AWS"));

AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;

AWSConfigs.AWSRegion = builder.Configuration.GetSection("AWS")["Region"];
    
builder.Services.AddSingleton<ICoreAmazonS3>(sp =>
{
    var config = sp.GetRequiredService<IOptions<AwsConfig>>().Value;
    var credentials = new BasicAWSCredentials(config.AccessKeyId, config.SecretAccessKey);
    return new AmazonS3Client(credentials);
});

builder.Services.AddSingleton<IProfilePictureService, S3ProfilePictureService>();
builder.Services.AddSingleton<IEventPlacePictureService, S3EventPlacePictureService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=dev.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.SetIsOriginAllowed(o => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddHostedService<EventStatusCleanupService>();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    Seeder.SeedPlaces(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();


#region Account endpoints

app.MapPost("/api/account/register", async (UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RegisterModel model) =>
{
    var user = new ApplicationUser
    {
        UserName = model.UserName,
        Gender = model.Gender,
        Email = model.Email,
        EventStatus = new EventStatus()
    };

    var result = await userManager.CreateAsync(user, model.Password);
    
    if (!result.Succeeded) return Results.BadRequest(result.Errors);
    
    await signInManager.SignInAsync(user, true);
    return Results.Ok();
});

app.MapPost("/api/account/login", async (UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, LoginModel model) =>
{
    var user = await userManager.FindByEmailAsync(model.Email);
    if (user == null) return Results.BadRequest("Invalid login attempt.");
    
    var result = await signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: false);
    
    return !result.Succeeded ? Results.BadRequest("Invalid login attempt.") : Results.Ok();
});

app.MapPost("/api/account/update_profile", async (HttpContext context, UserManager<ApplicationUser> userManager, ProfileModel model) =>
{
    var user = await userManager.FindByNameAsync(context.User.Identity.Name);
    if (user == null) return Results.BadRequest("User not found.");
    
    user.Name = model.Name;
    user.IgHandle = model.IgHandle;
    user.Description = model.Description;
    user.Gender = model.Gender;
    
    await userManager.UpdateAsync(user);
    
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/account/info", async (HttpContext context, UserManager<ApplicationUser> UserManager) =>
{
    if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();
    
    var user = await UserManager.Users
        .Include(u => u.EventStatus)
        .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
    
    return Results.Ok(new UserDto(user));
});

app.MapPost("/api/account/update_pfp", async (IFormFile file, HttpContext context, IProfilePictureService pfpService) => 
{
    await pfpService.UploadProfilePictureAsync(file.OpenReadStream(), context.User.Identity.Name);
    return Results.Ok();
}).DisableAntiforgery();

#endregion

#region Acount Acccess endpoints 

app.MapGet("/api/access_pfp", (string userName, IProfilePictureService pfpService) => Results.Ok(pfpService.GetDownloadUrl(userName)));

#endregion

#region Info endpoints

app.MapGet("/api/get_locations", () =>
{
    return Enum.GetValues<Location>().Select(location => new LocationDto(location)).ToList();
});

#endregion

#region Event endpoints 

app.MapPost("/api/register_event", async (UserManager<ApplicationUser> userManager, HttpContext context, Location location, DateTime time) =>
{
    var user = await userManager.Users
        .Include(u => u.EventStatus)
        .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
    if (user == null) return Results.Unauthorized();

    if (time < DateTime.Today)
        return Results.BadRequest();
    
    user.EventStatus.Active = true;
    user.EventStatus.Time = time;
    user.EventStatus.Location = location;
    user.EventStatus.With = [];
    
    await userManager.UpdateAsync(user);
    
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/cancel_event", async (UserManager<ApplicationUser> userManager, HttpContext context) =>
{
    var q = userManager.Users
        .Include(u => u.EventStatus);
    var user = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
    if (user == null) return Results.Unauthorized();
    
    user.EventStatus.Active = false;
    user.EventStatus.Time = null;
    user.EventStatus.Location = null;

    if (user.EventStatus.With != null)
    {
        foreach (var u in user.EventStatus.With)
        {
            var uQuery = await q.FirstOrDefaultAsync(u1 => u1.UserName == u);
            
            if (uQuery == null) continue;
            
            uQuery.EventStatus.With.Remove(u);
            
            await userManager.UpdateAsync(uQuery);
        }
    }
    
    user.EventStatus.With = null;
    
    await userManager.UpdateAsync(user);
    
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/invite_to_event", async (UserManager<ApplicationUser> userManager, List<string> invited, HttpContext context) =>
{
    var q = userManager.Users
        .Include(u => u.EventStatus);
    
    var invitor = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

    if (!invitor.EventStatus.Active) return Results.Unauthorized();
    
    foreach (var user in invited)
    {
        var u = await q.FirstOrDefaultAsync(u => u.UserName == user);
        if (u == null) return Results.BadRequest($"User {user} not found.");
        
        u.EventStatus.Active = true;
        u.EventStatus.Time = invitor.EventStatus.Time;
        u.EventStatus.Location = invitor.EventStatus.Location;
        var userInvited = new List<string>(invited);
        userInvited.Remove(u.UserName);
        userInvited.Add(invitor.UserName);
        u.EventStatus.With = userInvited;
        
        await userManager.UpdateAsync(u);
    }
    
    invitor.EventStatus.With = invited;
    
    await userManager.UpdateAsync(invitor);
    
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/query_visitors", async (UserManager<ApplicationUser> userManager, HttpContext context, int page) => {
    const int pageSize = 4;
    
    var user = await userManager.Users.Include(u => u.EventStatus).FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
    if (user == null) return Results.Unauthorized();

    if (!user.EventStatus.Active)
        return Results.Ok(new List<ApplicationUser>());
    
    var users = await userManager.Users
        .Include(u => u.EventStatus)
        .Where(u => u.EventStatus.Active == true && 
                                  u.EventStatus.Location == user.EventStatus.Location && 
                                  u.EventStatus.Time.Value.Day == user.EventStatus.Time.Value.Day)
        .Skip(page * pageSize)
        .Take(pageSize)
        .Select(u => new UserDto(u))
        .ToListAsync();
    
    return Results.Ok(users);
}).RequireAuthorization();

app.MapGet("/api/query_event_places", async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IEventPlacePictureService pictureService, HttpContext context) => {
    var user = await userManager.Users
        .Include(u => u.EventStatus)
        .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
        
    var places = await dbContext.Places
        .Where(p => p.Location == user.EventStatus.Location)
        .ToListAsync();
    
    var ret = places.Select(place => new EventPlaceDto(place, pictureService.GetDownloadUrls(place))).ToList();

    return Results.Ok(ret);
}).RequireAuthorization();

#endregion

app.Run();
