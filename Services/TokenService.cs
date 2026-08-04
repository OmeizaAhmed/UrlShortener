using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace UrlShortener.Services;

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

  public string GenerateToken(JwtInput input)
  {
    var credential = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), "HS256");

    var claims = new List<Claim>
    {
      new Claim(JwtRegisteredClaimNames.Sub, input.Id),
      new Claim(ClaimTypes.NameIdentifier, input.Id),
      new Claim(JwtRegisteredClaimNames.Email, input.Email),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      new Claim(JwtRegisteredClaimNames.FamilyName, input.LastName),
      new Claim(JwtRegisteredClaimNames.GivenName, input.FirstName)
      
    };
    foreach (var r in input.Role)
    {
      claims.Add(new Claim(ClaimTypes.Role, r));
    }

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

public class JwtInput
{
  public required string Id { get; set; }
  public required string Email { get; set; }
  public required string FirstName { get; set; }
  public required string LastName { get; set; }
  public required string[] Role { get; set; }
}