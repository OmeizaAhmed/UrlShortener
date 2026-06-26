
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

public class UrlShortenerContext : DbContext
{
  public DbSet<User> Users { get; set; }
  public DbSet<ShortUrl> ShortUrls { get; set; }
  public DbSet<ClickAnalytic> ClickAnalytics { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      string connectionString = $"Server=localhost;Database=UrlShortenerDB;User=root;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";

      var serverVersion = new MySqlServerVersion(new Version(8, 0, 46));

      optionsBuilder.UseMySql(connectionString, serverVersion);
    }
  protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique(); // Quick lookups for login
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
        });

        // 2. ShortUrl Configuration & Relationship with User
        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.OriginalUrl).IsRequired().HasMaxLength(2048);
            entity.Property(s => s.ShortCode).IsRequired().HasMaxLength(50);
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

            // Relationship: ShortUrl (1) -> ClickAnalytics (Many)
            entity.HasOne(c => c.ShortUrl)
                  .WithMany(s => s.ClickAnalytics)
                  .HasForeignKey(c => c.ShortUrlId)
                  .OnDelete(DeleteBehavior.Cascade); // Deleting URL deletes its click history
        });
}
}





