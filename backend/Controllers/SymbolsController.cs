using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/symbols")]
[Authorize]
public class SymbolsController(SymbolService symbolService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await symbolService.GetAllAsync());

    [HttpGet("{ticker}")]
    public async Task<IActionResult> Get(string ticker) => Ok(await symbolService.GetByTickerAsync(ticker));

    [HttpGet("{ticker}/candles")]
    public async Task<IActionResult> GetCandles(string ticker, [FromQuery] int limit = 200)
        => Ok(await symbolService.GetCandlesAsync(ticker, limit));

    [HttpGet("{ticker}/signals")]
    public async Task<IActionResult> GetSignals(string ticker)
        => Ok(await symbolService.GetSignalsAsync(ticker));
}
