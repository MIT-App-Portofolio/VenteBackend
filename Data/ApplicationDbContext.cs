using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<LocationInfo> Locations { get; set; }
    public DbSet<EventPlace> Places { get; set; }
    public DbSet<EventGroup> Groups { get; set; }
    public DbSet<Report> Reports { get; set; }
    
    public DbSet<ExitInstance> Exits { get; set; }
    
    public DbSet<CustomOffer> CustomOffers { get; set; }
    
    public DbSet<SharedAlbum> Albums { get; set; }
    
    public DbSet<Message> Messages { get; set; }
    public DbSet<GroupMessage> GroupMessages { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        DateTimeOffsetConverters(builder);
        
        base.OnModelCreating(builder);

        builder.Entity<EventPlace>()
            .HasOne(p => p.Owner)
            .WithOne(u => u.EventPlace)
            .HasForeignKey<EventPlace>(p => p.OwnerId);

        builder.Entity<EventPlaceEvent>()
            .HasOne(e => e.EventPlace)
            .WithMany(p => p.Events)
            .HasForeignKey(e => e.EventPlaceId);
        
        builder.Entity<EventPlaceOffer>()
            .HasOne(o => o.Event)
            .WithMany(e => e.Offers)
            .HasForeignKey(p => p.EventId);

        builder.Entity<ExitInstance>()
            .Property(e => e.Likes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, List<string>>>(v, (JsonSerializerOptions?)null)
            )
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, List<string>>>(
                (d1, d2) => JsonSerializer.Serialize(d1, (JsonSerializerOptions?)null) ==
                            JsonSerializer.Serialize(d2, (JsonSerializerOptions?)null),
                d => d == null ? 0 : JsonSerializer.Serialize(d, (JsonSerializerOptions?)null).GetHashCode(),
                d => JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                    JsonSerializer.Serialize(d, (JsonSerializerOptions?)null),
                    (JsonSerializerOptions?)null)));
            ;
            
        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Notifications)
            .WithOne()
            .HasForeignKey("ApplicationUserId")
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void DateTimeOffsetConverters(ModelBuilder builder)
    {
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            v => v.ToUniversalTime(), 
            v => v.ToUniversalTime()
        );

        var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : (DateTimeOffset?)null, 
            v => v.HasValue ? v.Value.ToUniversalTime() : (DateTimeOffset?)null
        );

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties();

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(DateTimeOffset))
                {
                    builder.Entity(entityType.Name).Property(property.Name)
                        .HasConversion(dateTimeOffsetConverter);
                }
                else if (property.PropertyType == typeof(DateTimeOffset?))
                {
                    builder.Entity(entityType.Name).Property(property.Name)
                        .HasConversion(nullableDateTimeOffsetConverter);
                }
            }
        }
    }
}