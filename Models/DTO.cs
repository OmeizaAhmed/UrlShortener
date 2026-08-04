using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Models
{
  public class RegisterInput
  {
    [EmailAddress]
    [Required]
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
  }

  public class LoginInput
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

  public class RefreshTokenResponse
  {
   public JwtResponse? Jwt { get; set; }
   public string? ErrorDescription { get; set; }
  }

  public class LongUrl
  {
    public required string Url { get; set; }
  }

  public class AddRoleInput
  {
    public required string Email { get; set; }
    public required string RoleName { get; set; }
  }



  public class UrlInfo
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

  public class SystemAnalyticsResponse
  {
    public required SystemOverview Overview { get; set; }
    public required SystemGrowth Growth { get; set; }
    public required SystemClicks Clicks { get; set; }
    public required SystemAnalytics Analytics { get; set; }
    public required List<TopUrl> TopUrls { get; set; }
  }

  public class SystemOverview
  {
    public required int TotalUsers { get; set; }
    public required int TotalUrls { get; set; }
    public required int ActiveUrls { get; set; }
    public required int ExpiredUrls { get; set; }
    public required int TotalClicks { get; set; }
  }

  public class SystemGrowth
  {
    public required int NewUsersToday { get; set; }
    public required int NewUsersThisWeek { get; set; }
    public required int NewUsersThisMonth { get; set; }
    public required int NewUrlsToday { get; set; }
    public required int NewUrlsThisWeek { get; set; }
    public required int NewUrlsThisMonth { get; set; }
  }

  public class SystemClicks
  {
    public required int Today { get; set; }
    public required int ThisWeek { get; set; }
    public required int ThisMonth { get; set; }
    public required double AveragePerUrl { get; set; }
  }

  public class SystemAnalytics
  {
    public required SystemOperatingSystems OperatingSystems { get; set; }
    public required SystemBrowsers Browsers { get; set; }
  }

  public class SystemOperatingSystems
  {
    public required int Windows { get; set; }
    public required int MacOS { get; set; }
    public required int Linux { get; set; }
    public required int Android { get; set; }
    public required int iOS { get; set; }
    public required int Other { get; set; }
  }


  public class SystemBrowsers
  {
    public required int Chrome { get; set; }
    public required int Safari { get; set; }
    public required int Firefox { get; set; }
    public required int Edge { get; set; }
    public required int Other { get; set; }
  }

  public class TopUrl
  {
    public required string ShortCode { get; set; }
    public required int Clicks { get; set; }
  }



}