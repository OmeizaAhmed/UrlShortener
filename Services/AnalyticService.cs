using UrlShortener.Models;
namespace UrlShortener.Services
{
    public interface IAnalyticService
    {
        Task LogClickAsync(int shortUrlId, string ipAddress, string userAgent);
    }

    public class AnalyticService : IAnalyticService
    {
        private readonly UrlShortenerContext _context;

        public AnalyticService(UrlShortenerContext context)
        {
            _context = context;
        }

        public async Task LogClickAsync(int shortUrlId, string ipAddress, string userAgent)
        {

            var clickAnalytic = new ClickAnalytic
            {
                ShortUrlId = shortUrlId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ClickedAt = DateTime.UtcNow
            };

            _context.ClickAnalytics.Add(clickAnalytic);
            await _context.SaveChangesAsync();
        }
    }
}