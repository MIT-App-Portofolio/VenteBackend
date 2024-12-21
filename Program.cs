using System.Security.Claims;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.SharedInterfaces;
using Amazon.S3;
using Amazon.Util;
using Microsoft.AspNetCore.Identity;
using Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server.Config;
using Server.Models;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=dev.db"));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapPost("/api/account/register", async (UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RegisterModel model) =>
{
    var user = new ApplicationUser
    {
        UserName = model.Email,
        IgHandle = model.IgHandle,
        Email = model.Email,
        Name = model.UserName,
        EventStatus = new EventStatus()
    };

    var result = await userManager.CreateAsync(user, model.Password);
    
    if (!result.Succeeded) return Results.BadRequest(result.Errors);
    
    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.Ok();
});

app.MapPost("/api/account/login", async (SignInManager<ApplicationUser> signInManager, LoginModel model) =>
{
    var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);
    
    return !result.Succeeded ? Results.BadRequest("Invalid login attempt.") : Results.Ok();
});

app.MapPost("/api/account/update_pfp", async (IFormFile file, HttpContext context, IProfilePictureService pfpService) => 
{
    if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();
    await pfpService.UploadProfilePictureAsync(file.OpenReadStream(), context.User.FindFirst(ClaimTypes.Email).Value);
    return Results.Ok();
}).DisableAntiforgery();

app.MapPost("/api/access_pfp", (string userName, IProfilePictureService pfpService) => Results.Ok(pfpService.GetDownloadUrl(userName)));

app.MapGet("/api/account/info", async (HttpContext context, UserManager<ApplicationUser> UserManager) =>
{
    if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();
    return Results.Ok(await UserManager.Users.Include(u => u.EventStatus).FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name));
});

app.MapPost("/api/register_event", async (UserManager<ApplicationUser> userManager, HttpContext context, Location location, DateTime time) =>
{
    if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();
    var user = await userManager.Users.Include(u => u.EventStatus).FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
    if (user == null) return Results.Unauthorized();
    
    user.EventStatus.Active = true;
    user.EventStatus.Time = time;
    user.EventStatus.Location = location;
    
    await userManager.UpdateAsync(user);
    
    return Results.Ok();
});

app.MapPost("/api/cancel_event", async (UserManager<ApplicationUser> userManager, HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();
    var user = await userManager.Users.Include(u => u.EventStatus).FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
    if (user == null) return Results.Unauthorized();
    
    user.EventStatus.Active = false;
    user.EventStatus.Time = null;
    user.EventStatus.Location = null;
    
    await userManager.UpdateAsync(user);
    
    return Results.Ok();
});

app.MapGet("/api/query_visitors", async (UserManager<ApplicationUser> userManager, HttpContext context, int page) => {
    const int pageSize = 4;
    const int hoursDiff = 5;
    
    if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();
    var user = await userManager.Users.Include(u => u.EventStatus).FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
    if (user == null) return Results.Unauthorized();

    if (!user.EventStatus.Active)
        return Results.Ok(new List<ApplicationUser>());
    
    var dateBefore = user.EventStatus.Time.Value.AddHours(-hoursDiff);
    var dateAfter = user.EventStatus.Time.Value.AddHours(hoursDiff);
    
    var users = await userManager.Users
        .Skip(page * pageSize)
        .Take(pageSize)
        .Include(u => u.EventStatus)
        .Where(u => u.EventStatus.Active == true && 
                                  u.EventStatus.Location == user.EventStatus.Location && 
                                  u.EventStatus.Time >= dateBefore && u.EventStatus.Time <= dateAfter)
        .ToListAsync();
    
    return Results.Ok(users);
});

app.Run();
