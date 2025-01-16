using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.SharedInterfaces;
using Amazon.S3;
using AwsSignatureVersion4;
using Microsoft.AspNetCore.Identity;
using Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server;
using Server.Config;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

// Use static web assets
if (builder.Environment.IsEnvironment("Sandbox"))
    builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorPages().AddRazorPagesOptions(opt =>
{
    opt.Conventions.AuthorizeFolder("/Admin", "RequireAdmin");
    opt.Conventions.AuthorizeFolder("/Affiliate", "RequireAffiliate");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
        options.UseSqlite("Data Source=dev.db");
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("Pg"));
});

// Auth
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]))
        };
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequireAffiliate", policy => policy.RequireRole("Affiliate"));

// Config services
builder.Services.AddHttpClient();

builder.Services.Configure<AwsConfig>(builder.Configuration.GetSection("AWS"));
builder.Services.Configure<AdminConfig>(builder.Configuration.GetSection("Admin"));
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JWT"));

// S3/AWS
AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
AWSConfigs.AWSRegion = builder.Configuration.GetSection("AWS")["Region"];

builder.Services.AddSingleton<ICoreAmazonS3>(sp =>
{
    var config = sp.GetRequiredService<IOptions<AwsConfig>>().Value;
    var credentials = new BasicAWSCredentials(config.AccessKeyId, config.SecretAccessKey);
    return new AmazonS3Client(credentials);
});

// Hetzner
var credentials = new ImmutableCredentials(builder.Configuration["Hetzner:AccessKeyId"], builder.Configuration["Hetzner:SecretAccessKey"], null);
builder.Services
    .AddTransient<AwsSignatureHandler>()
    .AddTransient(_ => new AwsSignatureHandlerSettings(builder.Configuration["Hetzner:Region"], "s3", credentials));
builder.Services
    .AddHttpClient("hetzner-storage")
    .AddHttpMessageHandler<AwsSignatureHandler>();

// Custom services
builder.Services.AddSingleton<IProfilePictureService, HetznerProfilePictureService>();
builder.Services.AddSingleton<IEventPlacePictureService, HetznerEventPlacePictureService>();
builder.Services.AddSingleton<JwtTokenManager>();
if (builder.Environment.IsEnvironment("Sandbox"))
    builder.Services.AddSingleton<SandboxEnvironmentSeeder>();

builder.Services.AddHostedService<EventStatusCleanupService>();
builder.Services.AddHostedService<OffersCleanupService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Role config
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("Affiliate"))
        await roleManager.CreateAsync(new IdentityRole("Affiliate"));
}

// Admin config
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminConfig = scope.ServiceProvider.GetRequiredService<IOptions<AdminConfig>>().Value;

    var admin = new ApplicationUser
    {
        UserName = "admin",
        Email = "admin@example.com",
        Gender = Gender.Male,
        HasPfp = false,
        EventStatus = new EventStatus()
    };

    await userManager.CreateAsync(admin, adminConfig.Password);
    await userManager.AddToRoleAsync(admin, "Admin");
}

if (app.Environment.IsEnvironment("Sandbox"))
{
    var seeder = app.Services.GetRequiredService<SandboxEnvironmentSeeder>();
    await seeder.Seed();
}

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapApiEndpoints();

app.Run();
