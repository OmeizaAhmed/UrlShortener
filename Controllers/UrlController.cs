
using System.Security.Claims;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UrlController : ControllerBase
{
  private UrlShortenerContext _context;

  private static readonly string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Random random = new Random();

  public UrlController(UrlShortenerContext context)
  {
    _context = context;
  }
  [HttpPost]
  public async Task<IActionResult> StoreUrl([FromBody] LongUrl url)
  {
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var longUrl = url.Url;
    if (string.IsNullOrEmpty(longUrl.Trim()))
    {
      return BadRequest();
    }
    var sanitizer = new HtmlSanitizer();
    longUrl = sanitizer.Sanitize(longUrl);
    var newUrl = new ShortUrl{OriginalUrl = longUrl, ShortCode = GenerateShortCode(), UserId = Convert.ToInt32(id)};
    _context.ShortUrls.Add(newUrl);
    await _context.SaveChangesAsync();
    
    return Ok(newUrl);
  }

  [HttpGet]
  public async Task<IActionResult> GetUrls()
  {
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(id == null)
    {
      return Unauthorized();
    }
    int userId = Convert.ToInt32(id);

    var urls = await _context.ShortUrls
      .Where(s => s.UserId == userId)
      .ToListAsync();

    return Ok(urls);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteUrl(int id)
  {
    var url = await _context.ShortUrls.FirstOrDefaultAsync(s => s.Id == id);
    if(url == null)
    {
      return NotFound();
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
      return Unauthorized();
    }
    var existingUrl = await _context.ShortUrls.FirstOrDefaultAsync(s => s.ShortCode == shortCode);
    if(existingUrl == null)
    {
      return NotFound();
    }
    if(existingUrl.UserId != Convert.ToInt32(userId))
    {
      return Forbid();
    }
    var sanitizer = new HtmlSanitizer();
    existingUrl.OriginalUrl = sanitizer.Sanitize(url.Url);
    await _context.SaveChangesAsync();
    return Ok(existingUrl);
  }

  private string GenerateShortCode(int length = 6)
    {
        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = chars[random.Next(chars.Length)];
        }
        return new string(buffer);
    }
}