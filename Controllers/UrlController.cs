
using System.Security.Claims;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
using UrlShortener.Common;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace UrlShortener.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UrlController : ControllerBase
{
  private readonly UrlShortenerContext _context;
  private readonly IDistributedCache _cacheService;

  private static readonly string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
  public UrlController(UrlShortenerContext context, IDistributedCache cacheService)
  {
    _context = context;
    _cacheService = cacheService;
  }
  [HttpPost]
  public async Task<IActionResult> StoreUrl([FromBody] LongUrl url)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(string.IsNullOrEmpty(userId))
    {
      return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", "User is not authorized"));
    }
    var longUrl = url.Url;
    if (string.IsNullOrWhiteSpace(longUrl))
    {
      return BadRequest(ApiResponse<object>.FailureResponse("Invalid URL", "URL cannot be empty"));
    }
    var sanitizer = new HtmlSanitizer();
    longUrl = sanitizer.Sanitize(longUrl);
    ShortUrl newUrl = new ShortUrl{OriginalUrl = longUrl, ShortCode = GenerateShortCode(), UserId = userId};
    _context.ShortUrls.Add(newUrl);
    await _context.SaveChangesAsync();
    
    return CreatedAtAction(nameof(GetUrl), new { shortCode = newUrl.ShortCode }, ApiResponse<ShortUrl>.SuccessResponse(newUrl, "URL stored successfully"));
  }

  [HttpGet]
  public async Task<IActionResult> GetUrls()
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(userId == null)
    {
      return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", "User is not authorized"));
    }

    List<UrlInfo> urls = await _context.ShortUrls
      .Where(s => s.UserId == userId)
      .Select(s => new UrlInfo
      {
        OriginalUrl = s.OriginalUrl,
        ShortCode = s.ShortCode,
        ClickCount = s.ClickCount,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
      }).OrderByDescending(s => s.UpdatedAt)
      .ToListAsync()
      ;
    

    return Ok(ApiResponse<List<UrlInfo>>.SuccessResponse(urls, "URLs retrieved successfully"));
  }
  [HttpGet("{shortCode}")]
  public async Task<IActionResult> GetUrl(string shortCode)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if(userId == null)
    {
      return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", "User is not authorized"));
    }
    
    // check if it is in cache
    var cacheKey = $"ShortUrlAnalytics_{shortCode}_{userId}";
    var cachedData = await _cacheService.GetStringAsync(cacheKey);
    if (!string.IsNullOrEmpty(cachedData))
    {
      var cachedResponse = JsonSerializer.Deserialize<ShortUrlAnalyticsResponse>(cachedData);
      if (cachedResponse != null)
      {
        return Ok(ApiResponse<ShortUrlAnalyticsResponse>.SuccessResponse(cachedResponse, "URL retrieved successfully"));
      }
    }

    var url = await _context.ShortUrls.FirstOrDefaultAsync(s => s.ShortCode == shortCode && s.UserId == userId);
    if(url == null)
    {
      return NotFound(ApiResponse<object>.FailureResponse("Not Found", "URL not found"));
    }

    var analytics = await _context.ClickAnalytics
      .Where(c => c.ShortUrlId == url.Id)
      .AsNoTracking()
      .ToListAsync();

    var now = DateTime.UtcNow;
    var oneDayAgo = now.AddDays(-1);
    var sevenDaysAgo = now.AddDays(-7);
    var thirtyDaysAgo = now.AddDays(-30);

    var totalClicks = analytics.Count;
    var uniqueVisitors = analytics
      .Select(c => c.IpAddress)
      .Where(ip => !string.IsNullOrWhiteSpace(ip) && ip != "Unknown")
      .Distinct()
      .Count();

    var clicksLast24Hours = analytics.Count(c => c.ClickedAt >= oneDayAgo);
    var clicksLast7Days = analytics.Count(c => c.ClickedAt >= sevenDaysAgo);
    var clicksLast30Days = analytics.Count(c => c.ClickedAt >= thirtyDaysAgo);
    var firstClickAt = analytics.OrderBy(c => c.ClickedAt).Select(c => (DateTime?)c.ClickedAt).FirstOrDefault();
    var lastClickAt = analytics.OrderByDescending(c => c.ClickedAt).Select(c => (DateTime?)c.ClickedAt).FirstOrDefault();

    var activeDays = Math.Max((now - url.CreatedAt).TotalDays, 1);
    var averageClicksPerDay = Math.Round(totalClicks / activeDays, 2);

    var response = new ShortUrlAnalyticsResponse
    {
      Id = url.Id,
      OriginalUrl = url.OriginalUrl,
      ShortCode = url.ShortCode,
      CreatedAt = url.CreatedAt,
      UpdatedAt = url.UpdatedAt,
      ExpiresAt = url.ExpiresAt,
      Kpis = new ShortUrlKpis
      {
        TotalClicks = totalClicks,
        UniqueVisitors = uniqueVisitors,
        ClicksLast24Hours = clicksLast24Hours,
        ClicksLast7Days = clicksLast7Days,
        ClicksLast30Days = clicksLast30Days,
        FirstClickAt = firstClickAt,
        LastClickAt = lastClickAt,
        AverageClicksPerDay = averageClicksPerDay
      }
    };
    // set time to live for cache
    var cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Cache for 5 minutes
    };
    await _cacheService.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), cacheOptions);
    
    return Ok(ApiResponse<ShortUrlAnalyticsResponse>.SuccessResponse(response, "URL retrieved successfully"));
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
    if(url.UserId != userId)
    {
      return Forbid();
    }
    _context.ShortUrls.Remove(url);
    await _context.SaveChangesAsync();
    // Invalidate the cache for this URL's analytics
    var cacheKey = $"ShortUrlAnalytics_{url.ShortCode}_{userId}";
    await _cacheService.RemoveAsync(cacheKey);
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
    if(existingUrl.UserId != userId)
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
    // Invalidate the cache for this URL's analytics
    var cacheKey = $"ShortUrlAnalytics_{shortCode}_{userId}";
    await _cacheService.RemoveAsync(cacheKey);
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