using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingBot.DTOs;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/symbols")]
[Authorize]
public class SymbolsController(SymbolService symbolService, MarketDataService marketData) : ControllerBase
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

    /// Add a new symbol (stock or fund). Fetches metadata + historical candles from Yahoo Finance.
    [HttpPost]
    public async Task<IActionResult> AddSymbol([FromBody] AddSymbolRequest req)
        => Ok(await symbolService.AddSymbolAsync(req.Ticker, req.Name, req.Sector, req.Type, marketData));

    /// Remove a symbol from the system (also removes it from all user watchlists).
    [HttpDelete("{ticker}")]
    public async Task<IActionResult> RemoveSymbol(string ticker)
    {
        await symbolService.RemoveSymbolAsync(ticker);
        return NoContent();
    }
}
