
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class UrlShortenerContext : IdentityDbContext<ApplicationUser>
{
  public UrlShortenerContext(DbContextOptions<UrlShortenerContext> options) : base(options)
  {
  }
  public DbSet<ShortUrl> ShortUrls { get; set; }
  public DbSet<ClickAnalytic> ClickAnalytics { get; set; }
  public DbSet<Refresh> Refreshes { get; set; }

  
  protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
        });
        // 2. ShortUrl Configuration & Relationship with User
        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.OriginalUrl).IsRequired().HasMaxLength(2048);
            entity.Property(s => s.ShortCode).HasMaxLength(50);
            entity.HasIndex(s => s.ShortCode).IsUnique(); // Quick lookups for redirects

            // Relationship: User (1) -> ShortUrls (Many)
            entity.HasOne(s => s.User)
                  .WithMany(u => u.ShortUrls)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // Deleting user deletes their URLs
        });

        // 3. ClickAnalytics Configuration & Relationship with ShortUrl
        modelBuilder.Entity<ClickAnalytic>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.IpAddress).HasMaxLength(45); // Fits IPv6 addresses
            entity.Property(c => c.UserAgent).HasMaxLength(500);
            entity.Property(c => c.OperatingSystem).HasMaxLength(100);
            entity.Property(c => c.Browser).HasMaxLength(100);

            // Relationship: ShortUrl (1) -> ClickAnalytics (Many)
            entity.HasOne(c => c.ShortUrl)
                  .WithMany(s => s.ClickAnalytics)
                  .HasForeignKey(c => c.ShortUrlId)
                  .OnDelete(DeleteBehavior.Cascade); // Deleting URL deletes its click history
        });

        modelBuilder.Entity<Refresh>(entity =>
        {
          entity.HasKey(r => r.Id);
          entity.Property(r => r.Token).HasMaxLength(256).IsRequired();
          entity.HasIndex(r => r.Token).IsUnique();
          entity.Property(r => r.Email).HasMaxLength(256).IsRequired();
          entity.HasIndex(r => r.Email).IsUnique();
        });
}
}





