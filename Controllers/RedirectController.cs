
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class RedirectController : ControllerBase
{
  private readonly UrlShortenerContext _context;

  public RedirectController(UrlShortenerContext context)
  {
    _context = context;
  }

  [HttpGet("{shortUrl}")]
  public async Task<IActionResult> RedirectToOriginalUrl(string shortUrl)
  {
    var entry = await _context.ShortUrls
      .FirstOrDefaultAsync(s => s.ShortCode == shortUrl);

    if (entry == null)
      return NotFound();

    // update click counter
    entry.ClickCount++;
    await _context.SaveChangesAsync();

    return Redirect(entry.OriginalUrl);
  }
}