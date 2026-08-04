namespace UrlShortener.Models
{
  public class ShortUrl
  {
    public int Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public int ClickCount { get; set; }
    public ICollection<ClickAnalytic> ClickAnalytics { get; set; } = new List<ClickAnalytic>();
  }
}
// http://localhost:5277/tK7eqF