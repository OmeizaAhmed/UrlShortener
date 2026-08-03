
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Common;
using Microsoft.AspNetCore.Authorization;
using UrlShortener.Models;
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public AdminController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("system-analytics")]
    public async Task<IActionResult> GetSystemAnalytics()
    {
        var systemAnalyticsResponse = await _dashboardService.GetSystemAnalyticsResponseAsync();
        return Ok(ApiResponse<SystemAnalyticsResponse>.SuccessResponse(systemAnalyticsResponse, "System analytics retrieved successfully"));
    }
}