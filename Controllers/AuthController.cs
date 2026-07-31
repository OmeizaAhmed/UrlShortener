using Ganss.Xss;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Common;
using UrlShortener.Models;
using UrlShortener.Services;
namespace UrlShortener.Controllers;
[ApiController]
[Route("api/[controller]")]
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

    if(oldUser != null) return BadRequest(ApiResponse<object>.FailureResponse("Email Exists", "email already exist, please login instead" ));

    string hashedPassword = _passwordService.HashPassword(cleanPassword);

    var newUser = new User{Email = cleanEmail, PasswordHash = hashedPassword};

    _context.Users.Add(newUser);
    await _context.SaveChangesAsync();

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == newUser.Email);

    if(user == null)
    {
      return BadRequest(ApiResponse<object>.FailureResponse("User Registration Failed", "Failed to register user, please try again"));
    }

    var jwtTokenInfo = await ProcessTokenAndCookies(Convert.ToString(user.Id), cleanEmail, "user");

    return Ok(ApiResponse<JwtResponse>.SuccessResponse(jwtTokenInfo, "User Registered Successfully"));
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
      return BadRequest(ApiResponse<object>.FailureResponse("Invalid Login", "Invalid Login credential check email and password"));
    }


    // compare password
    bool isValidPassword = _passwordService.VerifyPassword(cleanPassword, registeredUser.PasswordHash);

    if(!isValidPassword) return BadRequest(ApiResponse<object>.FailureResponse("Invalid Login", "Invalid Login credential check email and password"));
 

    // generate jwt and refresh token
    var token = await ProcessTokenAndCookies(Convert.ToString(registeredUser.Id), cleanEmail, "user");
    return Ok(ApiResponse<JwtResponse>.SuccessResponse(token, "Login Successful"));
  }

  [HttpPost("refresh")]
  public async Task<IActionResult> HandleRefresh()
  {
    // get refresh token from cookie
    HttpContext.Request.Cookies.TryGetValue("X-Refresh-Token", out string? refresh);
    if(string.IsNullOrEmpty(refresh)) return BadRequest(ApiResponse<object>.FailureResponse("Invalid Cookie", "Cookie Invalid or Empty"));

    // get refresh token from database
    var oldRefresh = await _context.Refreshes.FirstOrDefaultAsync(r => r.Token == refresh);
    if(oldRefresh == null) return BadRequest(ApiResponse<object>.FailureResponse("Invalid Refresh Token", "Refresh Token Invalid or Empty"));
        
    // check it has not yet expire
    if(oldRefresh.ExpireAt < DateTime.UtcNow) return BadRequest(ApiResponse<object>.FailureResponse("Expired Refresh Token", "Refresh Token has expired"));

    // validate if the user in the refresh is a registered user
    var registeredUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == oldRefresh.Email);

    if(registeredUser == null) return BadRequest(ApiResponse<object>.FailureResponse("Invalid Refresh Token", "User associated with refresh token not found"));
    // generate new jwt and new refresh

    var token = await ProcessTokenAndCookies(Convert.ToString(registeredUser.Id), registeredUser.Email, "user"); 
    
    return Ok(ApiResponse<JwtResponse>.SuccessResponse(token, "Token Refreshed Successfully"));
  }

  private async Task<JwtResponse> ProcessTokenAndCookies(string userId, string email, string role)
  {
    var token = _tokenService.GenerateToken(userId, email, role);
    // delete oldtokens
    var oldTokens = await _context.Refreshes.Where(r => r.Email == email).ToListAsync();
    _context.Refreshes.RemoveRange(oldTokens);
    await _context.SaveChangesAsync();

    // generate refresh
    var refresh = _tokenService.GenerateRefreshToken(email);
    _context.Refreshes.Add(new Refresh{Token = refresh.Token, Email = refresh.UserName});
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

