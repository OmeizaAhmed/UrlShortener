namespace UrlShortener.Models
{
  public class ShortUrl
  {
    public int Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public required User User { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public int ClickCount { get; set; }
    public ICollection<ClickAnalytic> ClickAnalytics = new List<ClickAnalytic>();
  }
}