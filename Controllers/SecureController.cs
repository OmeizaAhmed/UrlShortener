
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/{controller}")]
[Authorize]
public class SecureController : ControllerBase
{
  [HttpGet]
  public IActionResult SensitiveInfomation()
  {
    return Ok("If you are seeing this you're authorized");
  }
}