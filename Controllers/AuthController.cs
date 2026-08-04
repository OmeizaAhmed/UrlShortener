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
  private IAuthService _authService;

  public AuthController(IAuthService authService)
  {
    _authService = authService;
  }
  [HttpPost("register")]
  public async Task<IActionResult> RegisterUser([FromBody] RegisterInput userDetail)
  {
   var result = await _authService.RegisterUserAsync(userDetail);
    if(!result.Succeeded)
    {
      return BadRequest(ApiResponse<string>.FailureResponse("User Registration Failed", result.Errors.FirstOrDefault()?.Description ?? "User Registration Failed"));
    }
    return Ok(ApiResponse<string>.SuccessResponse("User Registered", "User Registered Successfully"));
  }

  [HttpPost("login")]
  public async Task<IActionResult> HandleLogin([FromBody] LoginInput loginDetail)
  {
    var token = await _authService.LoginUserAsync(loginDetail.Email, loginDetail.Password);
    if(token == null)
    {
      return BadRequest(ApiResponse<string>.FailureResponse("Login Failed", "Invalid Email or Password"));
    }
    return Ok(ApiResponse<JwtResponse>.SuccessResponse(token, "Login Successful"));
  }

  [HttpPost("refresh")]
  public async Task<IActionResult> HandleRefresh()
  {
    RefreshTokenResponse token = await _authService.HandleRefreshTokenAsync();
    if(token.ErrorDescription != null || token.Jwt == null)
    {
      return BadRequest(ApiResponse<string>.FailureResponse("Token Refresh Failed", token.ErrorDescription!));
    }   
    return Ok(ApiResponse<JwtResponse>.SuccessResponse(token.Jwt, "Token Refreshed Successfully"));
  }

}

