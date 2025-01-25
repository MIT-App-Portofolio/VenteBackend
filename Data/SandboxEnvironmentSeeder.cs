using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Bogus;

namespace Server.Data;

public partial class SandboxEnvironmentSeeder(ILogger<SandboxEnvironmentSeeder> logger, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) 
{
    private class RandomUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string PfpUrl { get; set; }
        public int Gender { get; set; }
        public DateTime BirthDate { get; set; }
    }
    
    public async Task Seed()
    {
        if (dbContext.Places.Any())
        {
            logger.LogInformation("Database already seeded.");
            return;
        }
        
        await SeedEventPlaces();
        await SeedUsers();
    }

    private async Task SeedEventPlaces()
    {
        logger.LogInformation("Seeding event places");
        
        var places = CreateRandomEventPlaces(10);
        using var client = new HttpClient();
        foreach (var place in places)
        {
            place.Images = [];
            var name = AlphaNumFilter().Replace(place.Name, "");
            
            var owner = new ApplicationUser
            {
                UserName = name,
                Name = place.Name,
                Email = new Faker().Internet.Email(),
                EventPlace = place,
                Gender = Gender.Male,
                BirthDate = DateTimeOffset.Now.AddYears(-18).ToUniversalTime(),
                HasPfp = false,
                EventStatus = new EventStatus
                {
                    Active = false,
                    Location = null,
                    Time = DateTimeOffset.Now.ToUniversalTime(),
                }
            };

            var result = await userManager.CreateAsync(owner, "Password123+");
                
            if (!result.Succeeded)
                throw new Exception("Could not create user. " + result);

            await userManager.AddToRoleAsync(owner, "Affiliate");
        }
    }
    
    private static List<EventPlace> CreateRandomEventPlaces(int count)
    {
        var faker = new Faker<EventPlace>("es")
            .RuleFor(p => p.Name, f => f.Company.CompanyName())
            .RuleFor(p => p.Description, f => f.Lorem.Sentence())
            .RuleFor(p => p.Location, f => f.PickRandom<Location>())
            .RuleFor(p => p.PriceRangeBegin, f => f.Random.Int(5, 20))
            .RuleFor(p => p.PriceRangeEnd, (f, p) => p.PriceRangeBegin + f.Random.Int(5, 20))
            .RuleFor(p => p.AgeRequirement, f => f.Random.Bool() ? f.PickRandom(16, 18) : null)
            .RuleFor(p => p.Events, f =>
            {
                var events = new List<EventPlaceEvent>();
                for (var i = 0; i < 10; i++)
                {
                    var offers = new List<EventPlaceOffer>();
                    
                    for (var j = 0; j < f.Random.Int(1, 5); j++)
                    {
                        offers.Add(new EventPlaceOffer
                        {
                            Name = f.Commerce.ProductName(),
                            Description = f.Lorem.Sentence(),
                            Price = f.Random.Int(10, 50),
                        });
                    }
                    
                    events.Add(new EventPlaceEvent
                    {
                        Time = RandomDate(),
                        Name = f.Commerce.ProductName(),
                        Description = f.Lorem.Sentence(),
                        Offers = offers
                    });
                }

                return events;
            });

        return faker.Generate(count);
    }

    private async Task SeedUsers()
    {
        logger.LogInformation("Seeding users");
        
        var users = CreateRandomUsers(100);

        foreach (var user in users)
        {
            var eventStatus = new EventStatus
            {
                Active = true,
                Location = new Faker().PickRandom(Enum.GetValues<Location>()),
                Time = RandomDate(),
            };

            var username = AlphaNumFilter().Replace(user.FirstName + user.LastName, "");
            
            var applicationUser = new ApplicationUser
            {
                UserName = username,
                Name = user.FirstName,
                Email = user.Email,
                Description = user.Description,
                IgHandle = username,
                HasPfp = true,
                BirthDate = user.BirthDate,
                EventStatus = eventStatus
            };
            
            var result = await userManager.CreateAsync(applicationUser, "Password123+");
            
            if (!result.Succeeded)
                throw new Exception("Could not create user. " + result);
        }
    }
    
    private static List<RandomUser> CreateRandomUsers(int count)
    {
        var faker = new Faker<RandomUser>("es")
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Description, f => f.Lorem.Sentence())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.PfpUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.Gender, f => f.PickRandom(0, 1))
            .RuleFor(u => u.BirthDate, f => f.Date.Past(30, DateTime.Now.AddYears(-18)));

        return faker.Generate(count);
    }
    
    private static DateTime RandomDate()
    {
        var random = new Random();
        var start = DateTime.Today + TimeSpan.FromDays(1);
        var end = DateTime.Today + TimeSpan.FromDays(14);
        var range = (end - start).Days;
        return start.AddDays(random.Next(range));
    }

    [GeneratedRegex(@"[^0-9a-zA-Z]+")]
    private static partial Regex AlphaNumFilter();
}
