using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Models
{
  public class RegisterInput
  {
    [EmailAddress]
    [Required]
    public required string Email { get; set; }
    public required string Password { get; set; }
  }

  public class JwtResponse
{
    public required string Token { get; set; }
    public required string ExpiresIn { get; set; }
    public required string TokenType { get; set; }
}

public class LongUrl
  {
    public required string Url { get; set; }
  }

}

public class urlInfo
{
  public required string OriginalUrl { get; set; }
  public required string ShortCode { get; set; }
  public required int ClickCount { get; set; }
  public required DateTime CreatedAt { get; set; }
  public required DateTime UpdatedAt { get; set; }
}

public class ShortUrlAnalyticsResponse
{
  public required int Id { get; set; }
  public required string OriginalUrl { get; set; }
  public required string ShortCode { get; set; }
  public required DateTime CreatedAt { get; set; }
  public required DateTime UpdatedAt { get; set; }
  public DateTime? ExpiresAt { get; set; }
  public required ShortUrlKpis Kpis { get; set; }
}

public class ShortUrlKpis
{
  public required int TotalClicks { get; set; }
  public required int UniqueVisitors { get; set; }
  public required int ClicksLast24Hours { get; set; }
  public required int ClicksLast7Days { get; set; }
  public required int ClicksLast30Days { get; set; }
  public DateTime? FirstClickAt { get; set; }
  public DateTime? LastClickAt { get; set; }
  public required double AverageClicksPerDay { get; set; }
}