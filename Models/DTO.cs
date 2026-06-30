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
    public string ExpiresIn { get; set; }
    public required string TokenType { get; set; }
}

}