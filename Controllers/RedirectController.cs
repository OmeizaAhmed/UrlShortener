namespace UrlShortener.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Services;
using UrlShortener.Common;
public class RedirectController : ControllerBase
{
  private readonly UrlShortenerContext _context;
  private readonly IAnalyticService _analyticService;

  public RedirectController(UrlShortenerContext context, IAnalyticService analyticService)
  {
    _context = context;
    _analyticService = analyticService;
  }

  [HttpGet("{shortUrl}")]
  public async Task<IActionResult> RedirectToOriginalUrl(string shortUrl)
  {
    var entry = await _context.ShortUrls
      .FirstOrDefaultAsync(s => s.ShortCode == shortUrl);

    if (entry == null)
      return NotFound(ApiResponse<object>.FailureResponse("Not Found", "Short URL not found"));

    // Increment click count
    entry.ClickCount++;
    await _context.SaveChangesAsync();

    // log click analytic
    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    var userAgent = Request.Headers["User-Agent"].ToString();
    await _analyticService.LogClickAsync(entry.Id, ipAddress, userAgent);

    return Redirect(entry.OriginalUrl);
  }
}