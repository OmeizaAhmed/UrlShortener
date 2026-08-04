
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Common;
using Microsoft.AspNetCore.Authorization;
using UrlShortener.Models;
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IAuthService _authService;

    public AdminController(IDashboardService dashboardService, IAuthService authService)
    {
        _dashboardService = dashboardService;
        _authService = authService;
    }

    [HttpGet("system-analytics")]
    public async Task<IActionResult> GetSystemAnalytics()
    {
        var systemAnalyticsResponse = await _dashboardService.GetSystemAnalyticsResponseAsync();
        return Ok(ApiResponse<SystemAnalyticsResponse>.SuccessResponse(systemAnalyticsResponse, "System analytics retrieved successfully"));
    }
    [HttpPost("create-role")]
    public async Task<IActionResult> CreateRole(string roleName)
    {
        await _authService.CreateRoleAsync(roleName);
        return Ok(ApiResponse<string>.SuccessResponse("Role created", $"{roleName} role was created successfully"));
    }
    [HttpPost("add-role-to-user")]
    public async Task<IActionResult> AddRoleToUser(AddRoleInput input)
    {
        Console.WriteLine($"Adding role {input.RoleName} to user {input.Email}");
        var result = await _authService.AddRoleToUserAsync(input.Email, input.RoleName);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<string>.FailureResponse("Failed to add role to user", result.Errors.FirstOrDefault()?.Description ?? "Failed to add role to user"));
        }

        return Ok(ApiResponse<string>.SuccessResponse("Role added to user", $"{input.RoleName} role was added to {input.Email} successfully"));
    }

}