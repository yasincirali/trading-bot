using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingBot.DTOs;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(TradingService tradingService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirst("userId")!.Value);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int limit = 50)
        => Ok(await tradingService.GetOrdersAsync(UserId, limit));

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest req)
    {
        var order = await tradingService.PlaceOrderAsync(UserId, req);
        return Created("", order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(Guid id)
        => Ok(await tradingService.CancelOrderAsync(UserId, id));
}
