using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Server.Data;
using Microsoft.EntityFrameworkCore;
using Server.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

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
        Email = model.Email,
        Name = model.UserName,
        Status = AccountStatus.WaitingSetup
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

app.MapGet("/api/account/info", async (HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();
    return Results.Ok(new
    {
        Name = context.User.Identity.Name,
        Email = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
    });
});

app.Run();
