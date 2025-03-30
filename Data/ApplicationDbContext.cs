using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<EventPlace> Places { get; set; }
    public DbSet<EventGroup> Groups { get; set; }
    public DbSet<Report> Reports { get; set; }
    
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