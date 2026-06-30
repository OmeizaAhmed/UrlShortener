namespace UrlShortener.Models
{
  public class Refresh
  {
    public int Id;
    public required string Token { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpireAt { get; set; } = DateTime.UtcNow.AddDays(7);
  }
}