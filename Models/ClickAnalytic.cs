namespace UrlShortener.Models
{
  public class ClickAnalytic
  {
    public int Id { get; set; }
    public int ShortUrlId { get; set; }

    public ShortUrl? ShortUrl { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
  }
}


