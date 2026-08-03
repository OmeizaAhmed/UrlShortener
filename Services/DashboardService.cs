using UrlShortener.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public interface IDashboardService
{
    Task<SystemAnalyticsResponse> GetSystemAnalyticsResponseAsync();
    Task<SystemAnalytics> GetSystemAnalyticsAsync();
    Task<SystemOverview> GetSystemOverviewAsync();
    Task<SystemGrowth> GetSystemGrowthAsync();
    Task<SystemClicks> GetSystemClicksAsync();
    Task<SystemOperatingSystems> GetSystemOperatingSystemsAsync();
    Task<SystemBrowsers> GetSystemBrowsersAsync();
    Task<List<TopUrl>> GetTopUrlsAsync(int topN = 10);
}

public class DashboardService : IDashboardService
{

    private readonly UrlShortenerContext _context;
    private static readonly string[] MajorOSList = { "Android", "iOS", "Windows", "Mac OS", "Linux" };
    private static readonly string[] MajorBrowserList = { "Chrome", "Safari", "Firefox", "Edge" };
    private readonly IDistributedCache _cacheService;

    public DashboardService(UrlShortenerContext context, IDistributedCache cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }
    public async Task<SystemAnalyticsResponse> GetSystemAnalyticsResponseAsync()
    {
        // Check if the analytics data is already cached
        var cachedData = await _cacheService.GetStringAsync("SystemAnalyticsResponse");
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedResponse = JsonSerializer.Deserialize<SystemAnalyticsResponse>(cachedData);
            if (cachedResponse != null)
            {
                return cachedResponse;
            }
        } 

        var overview = await GetSystemOverviewAsync();
        var growth = await GetSystemGrowthAsync();
        var clicks = await GetSystemClicksAsync();
        var analytics = await GetSystemAnalyticsAsync();
        var topUrls = await GetTopUrlsAsync();


        var response = new SystemAnalyticsResponse
        {
            Overview = overview,
            Growth = growth,
            Clicks = clicks,
            Analytics = analytics,
            TopUrls = topUrls
        };
        // set time to live for cache
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Cache for 10 minutes
        };

        // Cache the analytics data
        await _cacheService.SetStringAsync("SystemAnalyticsResponse", JsonSerializer.Serialize(response), cacheOptions);

        return response;
    }

    public async Task<SystemOverview> GetSystemOverviewAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalUrls = await _context.ShortUrls.CountAsync();
        var activeUrls = await _context.ShortUrls.CountAsync(s => s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow);
        var totalClicks = await _context.ClickAnalytics.CountAsync();
        return new SystemOverview
        {
            TotalUsers = totalUsers,
            TotalUrls = totalUrls,
            TotalClicks = totalClicks,
            ActiveUrls = activeUrls,
            ExpiredUrls = totalUrls - activeUrls
        };
    }

    public async Task<SystemGrowth> GetSystemGrowthAsync()
    {
        var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.Date);
        var newUrlsToday = await _context.ShortUrls.CountAsync(s => s.CreatedAt >= DateTime.UtcNow.Date);
        var newUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7));
        var newUrlsThisWeek = await _context.ShortUrls.CountAsync(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-7));
        var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1));
        var newUrlsThisMonth = await _context.ShortUrls.CountAsync(s => s.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

        return new SystemGrowth
        {
            NewUsersToday = newUsersToday,
            NewUrlsToday = newUrlsToday,
            NewUsersThisWeek = newUsersThisWeek,
            NewUrlsThisWeek = newUrlsThisWeek,
            NewUsersThisMonth = newUsersThisMonth,
            NewUrlsThisMonth = newUrlsThisMonth
        };

    }

    public async Task<SystemClicks> GetSystemClicksAsync()
    {
        var clicksToday = await _context.ClickAnalytics.CountAsync(c => c.ClickedAt >= DateTime.UtcNow.Date);
        var clicksThisWeek = await _context.ClickAnalytics.CountAsync(c => c.ClickedAt >= DateTime.UtcNow.AddDays(-7));
        var clicksThisMonth = await _context.ClickAnalytics.CountAsync(c => c.ClickedAt >= DateTime.UtcNow.AddMonths(-1));
        var totalClicks = await _context.ClickAnalytics.CountAsync();
        var totalUrls = await _context.ShortUrls.CountAsync();
        var averagePerUrl = totalUrls > 0 ? (double)totalClicks / totalUrls : 0;
        return new SystemClicks
        {
            Today = clicksToday,
            ThisWeek = clicksThisWeek,
            ThisMonth = clicksThisMonth,
            AveragePerUrl = averagePerUrl
        };
    }
    public async Task<SystemAnalytics> GetSystemAnalyticsAsync()
    {
        return new SystemAnalytics
        {
            OperatingSystems = await GetSystemOperatingSystemsAsync(),
            Browsers = await GetSystemBrowsersAsync()
        };
    }

    public async Task<SystemOperatingSystems> GetSystemOperatingSystemsAsync()
    {
        var androidCount = await _context.ClickAnalytics.CountAsync(c => c.OperatingSystem == "Android");
        var iosCount = await _context.ClickAnalytics.CountAsync(c => c.OperatingSystem == "iOS");
        var windowsCount = await _context.ClickAnalytics.CountAsync(c => c.OperatingSystem == "Windows");
        var macosCount = await _context.ClickAnalytics.CountAsync(c => c.OperatingSystem == "Mac OS");
        var linuxCount = await _context.ClickAnalytics.CountAsync(c => c.OperatingSystem == "Linux");
        var otherCount = await _context.ClickAnalytics.CountAsync(c => !MajorOSList.Contains(c.OperatingSystem));

        return new SystemOperatingSystems
        {
            Android = androidCount,
            iOS = iosCount,
            Windows = windowsCount,
            MacOS = macosCount,
            Linux = linuxCount,
            Other = otherCount
        };
    }

    public async Task<SystemBrowsers> GetSystemBrowsersAsync()
    {
        var chromeCount = await _context.ClickAnalytics.CountAsync(c => c.Browser.StartsWith("Chrome"));
        var safariCount = await _context.ClickAnalytics.CountAsync(c => c.Browser == "Safari");
        var firefoxCount = await _context.ClickAnalytics.CountAsync(c => c.Browser == "Firefox");
        var edgeCount = await _context.ClickAnalytics.CountAsync(c => c.Browser == "Edge");
        var otherCount = await _context.ClickAnalytics.CountAsync(c => !MajorBrowserList.Contains(c.Browser));

        return new SystemBrowsers
        {
            Chrome = chromeCount,
            Safari = safariCount,
            Firefox = firefoxCount,
            Edge = edgeCount,
            Other = otherCount
        };
    }

    public async Task<List<TopUrl>> GetTopUrlsAsync(int topN = 10)
    {
        var topUrls = await _context.ShortUrls
    .OrderByDescending(s => s.ClickAnalytics.Count) // Sorts directly on the count
    .Take(topN)
    .Select(s => new TopUrl
    {
        ShortCode = s.ShortCode,
        Clicks = s.ClickAnalytics.Count
    })
    .ToListAsync();

        return topUrls;
    }

}