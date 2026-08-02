using UrlShortener.Models;
using UAParser;
namespace UrlShortener.Services
{
    public interface IAnalyticService
    {
        Task LogClickAsync(int shortUrlId, string ipAddress, string userAgent);
    }

    public class AnalyticService : IAnalyticService
    {
        private readonly UrlShortenerContext _context;

        private readonly Parser _parser;

        public AnalyticService(UrlShortenerContext context, Parser parser)
        {
            _context = context;
            _parser = parser;
        }

        public async Task LogClickAsync(int shortUrlId, string ipAddress, string userAgent)
        {
            var clientInfo = _parser.Parse(userAgent);
            var clickAnalytic = new ClickAnalytic
            {
                ShortUrlId = shortUrlId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OperatingSystem = clientInfo?.OS?.Family ?? "Unknown",
                Browser = clientInfo?.Browser?.Family ?? "Unknown",
                ClickedAt = DateTime.UtcNow
            };

            _context.ClickAnalytics.Add(clickAnalytic);
            await _context.SaveChangesAsync();
        }
    }
}