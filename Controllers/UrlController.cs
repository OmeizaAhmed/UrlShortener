
using System.Security.Claims;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
using UrlShortener.Common;

namespace UrlShortener.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UrlController : ControllerBase
{
  private UrlShortenerContext _context;

  private static readonly string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
  public UrlController(UrlShortenerContext context)
  {
    _context = context;
  }
  [HttpPost]
  public async Task<IActionResult> StoreUrl([FromBody] LongUrl url)
  {
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var longUrl = url.Url;
    if (string.IsNullOrWhiteSpace(longUrl))
    {
      return BadRequest(ApiResponse<object>.FailureResponse("Invalid URL", "URL cannot be empty"));
    }
    var sanitizer = new HtmlSanitizer();
    longUrl = sanitizer.Sanitize(longUrl);
    ShortUrl newUrl = new ShortUrl{OriginalUrl = longUrl, ShortCode = GenerateShortCode(), UserId = Convert.ToInt32(id)};
    _context.ShortUrls.Add(newUrl);
    await _context.SaveChangesAsync();
    
    return CreatedAtAction(nameof(GetUrl), new { shortCode = newUrl.ShortCode }, ApiResponse<ShortUrl>.SuccessResponse(newUrl, "URL stored successfully"));
  }

  [HttpGet]
  public async Task<IActionResult> GetUrls()
  {
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(id == null)
    {
      return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", "User is not authorized"));
    }
    int userId = Convert.ToInt32(id);

    List<urlInfo> urls = await _context.ShortUrls
      .Where(s => s.UserId == userId)
      .Select(s => new urlInfo
      {
        OriginalUrl = s.OriginalUrl,
        ShortCode = s.ShortCode,
        ClickCount = s.ClickCount,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
      }).OrderByDescending(s => s.UpdatedAt)
      .ToListAsync()
      ;
    

    return Ok(ApiResponse<List<urlInfo>>.SuccessResponse(urls, "URLs retrieved successfully"));
  }
  [HttpGet("{shortCode}")]
  public async Task<IActionResult> GetUrl(string shortCode)
  {
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(id == null)
    {
      return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", "User is not authorized"));
    }
    int userId = Convert.ToInt32(id);

    var url = await _context.ShortUrls.FirstOrDefaultAsync(s => s.ShortCode == shortCode && s.UserId == userId);
    if(url == null)
    {
      return NotFound(ApiResponse<object>.FailureResponse("Not Found", "URL not found"));
    }
    
    return Ok(ApiResponse<ShortUrl>.SuccessResponse(url, "URL retrieved successfully"));
  }
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteUrl(int id)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(userId == null)
    {
      return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", "User is not authorized"));
    }
    var url = await _context.ShortUrls.FirstOrDefaultAsync(s => s.Id == id);
    if(url == null)
    {
      return NotFound(ApiResponse<object>.FailureResponse("Not Found", "URL not found"));
    }
    if(url.UserId != Convert.ToInt32(userId))
    {
      return Forbid();
    }
    _context.ShortUrls.Remove(url);
    await _context.SaveChangesAsync();
    return NoContent();
  }
  [HttpPut("{shortCode}")]
  public async Task<IActionResult> UpdateUrl(string shortCode, [FromBody] LongUrl url)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(userId == null)
    {
      return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", "User is not authorized"));
    }
    var existingUrl = await _context.ShortUrls.FirstOrDefaultAsync(s => s.ShortCode == shortCode);
    if(existingUrl == null)
    {
      return NotFound(ApiResponse<object>.FailureResponse("Not Found", "URL not found"));
    }
    if(existingUrl.UserId != Convert.ToInt32(userId))
    {
      return Forbid();
    }
    var sanitizer = new HtmlSanitizer();
    if(string.IsNullOrWhiteSpace(url.Url))
    {
      return BadRequest(ApiResponse<object>.FailureResponse("Invalid URL", "URL cannot be empty"));
    }
    existingUrl.OriginalUrl = sanitizer.Sanitize(url.Url);
    existingUrl.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    return Ok(ApiResponse<ShortUrl>.SuccessResponse(existingUrl, "URL updated successfully"));
  }

  private string GenerateShortCode(int length = 6)
    {
        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = chars[Random.Shared.Next(chars.Length)];
        }
        return new string(buffer);
    }
}