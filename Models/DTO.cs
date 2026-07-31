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