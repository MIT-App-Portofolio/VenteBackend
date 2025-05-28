using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.SharedInterfaces;
using Amazon.S3;
using AwsSignatureVersion4;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Api;
using Server.Config;
using Server.Hubs;
using Server.ManualMigrations;
using Server.Services.Concrete;
using Server.Services.Hosted;
using Server.Services.Implementations;
using Server.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

// Use static web assets
if (builder.Environment.IsEnvironment("Sandbox"))
    builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorPages().AddRazorPagesOptions(opt =>
{
    opt.Conventions.AuthorizeFolder("/Admin", "RequireAdmin");
    opt.Conventions.AuthorizeFolder("/User", "RequireLoggedIn");
    opt.Conventions.AuthorizeFolder("/Affiliate", "RequireAffiliate");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    
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
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSignalR();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequireAffiliate", policy => policy.RequireRole("Affiliate"))
    .AddPolicy("RequireLoggedIn", policy => policy.RequireAuthenticatedUser());

// Config services
builder.Services.AddHttpClient();

builder.Services.Configure<GoogleConfig>(builder.Configuration.GetSection("Google"));
builder.Services.Configure<AppleConfig>(builder.Configuration.GetSection("Apple"));
builder.Services.Configure<AwsConfig>(builder.Configuration.GetSection("AWS"));
builder.Services.Configure<AdminConfig>(builder.Configuration.GetSection("Admin"));
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JWT"));

// Firebase
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.GetApplicationDefault(),
});

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
builder.Services.AddSingleton<ILocationImageService, HetznerLocationImageService>();
builder.Services.AddSingleton<ICustomOfferPictureService, HetznerCustomOfferPictureService>();
builder.Services.AddSingleton<IAlbumPictureService, HetznerAlbumPictureService>();
builder.Services.AddSingleton<JwtTokenManager>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<AppleTokenValidatorService>();
builder.Services.AddSingleton<CustomOfferTokenStorage>();
builder.Services.AddSingleton<ExitFeeds>();
builder.Services.AddSingleton<MessagingConnectionMap>();
builder.Services.AddSingleton<ShadowedUsersTracker>();

builder.Services.AddHostedService<EventStatusCleanupService>();
builder.Services.AddHostedService<NoteCleanupService>();
builder.Services.AddHostedService<EventsCleanupService>();
builder.Services.AddHostedService<CustomOffersCleanupService>();
builder.Services.AddHostedService<AlbumCleanupService>();
builder.Services.AddHostedService<ExitFeedExecutor>();
builder.Services.AddHostedService<ExitCleanupService>();

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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    
    var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ExitSystemMigration>>();
    await ExitSystemMigration.Migrate(db, um, logger);

    var picService = scope.ServiceProvider.GetRequiredService<IEventPlacePictureService>();
    var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    var iconfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await EventPictureEscapingMigration.Migrate(db, picService, iconfig, httpFactory);
}

using (var scope = app.Services.CreateScope())
{
    var feed = scope.ServiceProvider.GetRequiredService<ExitFeeds>();
    feed.Enqueue("salou");
    feed.Enqueue("sabadell");
    await feed.ExecuteQueue();
}

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

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByEmailAsync("appletesting@example.com") == null)
    {
        await userManager.CreateAsync(new ApplicationUser
        {
            UserName = "appletest",
            Email = "appletesting@example.com",
            BirthDate = DateTimeOffset.UnixEpoch,
            Gender = Gender.Male,
            EventStatus = new EventStatus
            {
                Active = false,
            },
        }, "AppleTestingAccount1234+");
    }
    if (await userManager.FindByEmailAsync("googletesting@example.com") == null)
    {
        await userManager.CreateAsync(new ApplicationUser
        {
            UserName = "googletest",
            Email = "googletesting@example.com",
            BirthDate = DateTimeOffset.UnixEpoch,
            Gender = Gender.Male,
            EventStatus = new EventStatus
            {
                Active = false,
            },
        }, "GoogleTestingAccount1234+");
    }
}

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Sandbox"))
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

app.MapHub<ChatHub>("/chathub");

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/online", () => Results.Ok());

app.MapApiEndpoints();

app.Run();
