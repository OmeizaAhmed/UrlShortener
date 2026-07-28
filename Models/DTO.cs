using System.ComponentModel.DataAnnotations;
using AngleSharp.Dom;

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