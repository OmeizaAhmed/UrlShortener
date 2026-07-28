using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UrlShortener.Models;

public class TokenServices
{
  private string _key;
  private string _issuer;
  private string _audience;
  private int _lifeTime;

  public TokenServices(IConfiguration configuration)
  {
    _key = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? throw new Exception("Key cannot be empty check user secret");
    _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")?? throw new Exception("Issuer cannot be empty check user secret");
    _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new Exception("Audience cannot be empty check user secret");
    _lifeTime = Convert.ToInt32(Environment.GetEnvironmentVariable("JWT_LIFETIME"));
  }

  public string GenerateToken(string userId, string username, string role)
  {
    var credential = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), "HS256");

    var claims = new[]
    {
      new Claim(JwtRegisteredClaimNames.Sub, userId),
      new Claim(JwtRegisteredClaimNames.Email, username),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      new Claim("role", role)
    };

    var token = new JwtSecurityToken(
      audience: _audience,
      issuer: _issuer,
      expires: DateTime.UtcNow.AddMinutes(_lifeTime),
      claims: claims,
      signingCredentials: credential
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public RefreshToken GenerateRefreshToken(string username)
  {
    return new RefreshToken
    {
      Token = Guid.NewGuid().ToString(),
      UserName = username
    };
  }
}

public class RefreshToken
{
  public required string Token { get; set; }
  public required string UserName { get; set; }

}