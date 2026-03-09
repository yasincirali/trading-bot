using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public class PortfolioController(TradingService tradingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = Guid.Parse(User.FindFirst("userId")!.Value);
        return Ok(await tradingService.GetPortfolioAsync(userId));
    }
}
