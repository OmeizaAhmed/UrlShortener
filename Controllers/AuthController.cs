using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
[ApiController]
[Route("api/{controller}")]
public class AuthController: ControllerBase
{
  private UrlShortenerContext _context;
  private IPasswordService _passwordService;
  private TokenServices _tokenService;

  public AuthController(UrlShortenerContext context, IPasswordService passwordService, TokenServices tokenServices)
  {
    _context = context;
    _passwordService = passwordService;
    _tokenService = tokenServices;
  }
  [HttpPost("register")]
  public async Task<IActionResult> RegisterUser([FromBody] RegisterInput userDetail)
  {
    var sanitizer = new HtmlSanitizer();

    string cleanEmail = sanitizer.Sanitize(userDetail.Email);
    string cleanPassword = sanitizer.Sanitize(userDetail.Password.Trim());

    var oldUser = await _context.Users.FirstOrDefaultAsync( u => u.Email == cleanEmail);

    if(oldUser != null) return BadRequest();

    string hashedPassword = _passwordService.HashPassword(cleanPassword);

    var newUser = new User{Email = cleanEmail, PasswordHash = hashedPassword};

    await _context.Users.AddAsync(newUser);
    await _context.SaveChangesAsync();

    var jwtTokenInfo = await ProcessTokenAndCookies(cleanEmail, "user");

    return Ok(jwtTokenInfo);
  }

  [HttpPost("login")]
  public async Task<IActionResult> HandleLogin([FromBody] RegisterInput loginDetail)
  {
    // sanitize input
    var sanitizer = new HtmlSanitizer();

    string cleanEmail = sanitizer.Sanitize(loginDetail.Email);
    string cleanPassword = sanitizer.Sanitize(loginDetail.Password.Trim());

    // check if user is a registered account
    var registeredUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == cleanEmail);

    if(registeredUser == null)
    {
      return BadRequest("Invalid Login credential check email and password");
    }


    // compare password
    bool isValidPassword = _passwordService.VerifyPassword(cleanPassword, registeredUser.PasswordHash);

    if(!isValidPassword) return BadRequest("Invalid Login credential check email and password");
 

    // generate jwt and refresh token
    var token = await ProcessTokenAndCookies(cleanEmail, "user");
    return Ok(token);
  }

  [HttpPost("refresh")]
  public async Task<IActionResult> HandleRefresh()
  {
    // get refresh token from cookie
    HttpContext.Request.Cookies.TryGetValue("X-Refresh-Token", out string? refresh);
    if(string.IsNullOrEmpty(refresh)) return BadRequest("Cookie Invalid or Empyty");

    // get refresh token from database
    var oldRefresh = await _context.Refreshes.FirstOrDefaultAsync(r => r.Token == refresh);
    if(oldRefresh == null) return BadRequest("Invalid Refresh Cookie");
        
    // check it has not yet expire
    if(oldRefresh.ExpireAt < DateTime.UtcNow) return BadRequest("Expired Refresh Token");

    // validate if the user in the refresh is a registered user
    var registeredUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == oldRefresh.Email);

    if(registeredUser == null) return BadRequest();
    // generate new jwt and new refresh

    var token = await ProcessTokenAndCookies(registeredUser.Email, "user"); 
    
    return Ok(token);
  }

  private async Task<JwtResponse> ProcessTokenAndCookies(string email, string role)
  {
    var token = _tokenService.GenerateToken(email, role);
    // delete oldtokens
    var oldTokens = _context.Refreshes.Where(r => r.Email == email).ToList();
    _context.Refreshes.RemoveRange(oldTokens);
    await _context.SaveChangesAsync();

    // generate refresh
    var refresh = _tokenService.GenerateRefreshToken(email);
    await _context.Refreshes.AddAsync(new Refresh{Token = refresh.Token, Email = refresh.UserName});
    await _context.SaveChangesAsync();
    HttpContext.Response.Cookies.Append("X-Refresh-Token", refresh.Token, new CookieOptions
    {
      SameSite = SameSiteMode.Strict,
      HttpOnly = true,
      Expires = DateTimeOffset.UtcNow.AddDays(7)
    });

    return new JwtResponse 
        { 
            Token = token, 
            ExpiresIn = Environment.GetEnvironmentVariable("JWT_LIFETIME") ?? "", 
            TokenType = "Bearer" 
        };
  }
}

