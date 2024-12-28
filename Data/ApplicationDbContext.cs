using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EventPlace>()
            .HasOne(p => p.Owner)
            .WithOne(u => u.EventPlace)
            .HasForeignKey<EventPlace>(p => p.OwnerId);
    }

    public DbSet<EventPlace> Places { get; set; }
}