namespace Server.Data;

public class Seeder
{
    public static void SeedPlaces(ApplicationDbContext context)
    {
        if (context.Places.Any())
            return;

        var places = new List<EventPlace>
        {
            new()
            {
                Location = Location.Salou,
                Name = "Cage",
                Description = "Un bar en Salou",
                Images = ["cg-1.jpeg", "cg-2.jpg", "cg-3.jpeg"],
                PriceRangeBegin = 10,
                PriceRangeEnd = 15,
            },
            new()
            {
                Location = Location.Salou,
                Name = "Cafe Di Mare",
                Description = "Un cafe en Salou",
                Images = ["cdm-1.jpg", "cdm-2.jpg", "cdm-3.png"],
                PriceRangeBegin = 10,
                PriceRangeEnd = 20,
            },
            new()
            {
                Location = Location.Zaragoza,
                Name = "Pilar",
                Description = "Ns que es esto",
                Images = [],
                PriceRangeBegin = 0,
                PriceRangeEnd = 0,
            },
        };

        context.Places.AddRange(places);
        context.SaveChanges();
    }
}