using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EventPlace>()
            .HasOne(p => p.Owner)
            .WithOne(u => u.EventPlace)
            .HasForeignKey<EventPlace>(p => p.OwnerId);
        
        builder.Entity<EventPlaceOffer>()
            .HasOne(o => o.EventPlace)
            .WithMany(p => p.Offers)
            .HasForeignKey(p => p.EventPlaceId);
    }

    public DbSet<EventPlace> Places { get; set; }
}