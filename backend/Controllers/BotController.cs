using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingBot.Bot;
using TradingBot.Data;
using TradingBot.DTOs;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/bot")]
[Authorize]
public class BotController(AppDbContext db, TradingEngine engine, IConfiguration config) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirst("userId")!.Value);

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var cfg = await db.BotConfigs.FirstOrDefaultAsync(c => c.UserId == UserId);
        var paperTrading = config.GetValue<bool>("Trading:PaperTradingMode", true);
        return Ok(new BotStatusDto(engine.IsBotRunning(UserId), cfg == null ? null : ToDto(cfg), paperTrading));
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start()
    {
        await db.BotConfigs
            .Where(c => c.UserId == UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Enabled, true).SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        engine.StartBot(UserId);
        return Ok(new { running = true, message = "Bot started" });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        await db.BotConfigs
            .Where(c => c.UserId == UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Enabled, false).SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        engine.StopBot(UserId);
        return Ok(new { running = false, message = "Bot stopped" });
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var cfg = await db.BotConfigs.FirstOrDefaultAsync(c => c.UserId == UserId)
            ?? throw new Services.AppException("Bot config not found", 404);
        return Ok(ToDto(cfg));
    }

    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateBotConfigRequest req)
    {
        var cfg = await db.BotConfigs.FirstOrDefaultAsync(c => c.UserId == UserId)
            ?? throw new Services.AppException("Bot config not found", 404);

        if (req.ConfidenceThreshold.HasValue) cfg.ConfidenceThreshold = req.ConfidenceThreshold.Value;
        if (req.MaxOrderSizeTry.HasValue) cfg.MaxOrderSizeTry = req.MaxOrderSizeTry.Value;
        if (req.DailyLossLimitTry.HasValue) cfg.DailyLossLimitTry = req.DailyLossLimitTry.Value;
        if (req.TickIntervalMs.HasValue) cfg.TickIntervalMs = req.TickIntervalMs.Value;
        if (req.Watchlist != null) cfg.Watchlist = req.Watchlist;
        cfg.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(cfg));
    }

    /// Add a ticker to the watchlist immediately (bot picks it up on next tick).
    /// If the symbol doesn't exist in the system, it is created automatically from Yahoo Finance.
    [HttpPost("watchlist/{ticker}")]
    public async Task<IActionResult> AddToWatchlist(string ticker,
        [FromServices] SymbolService symbolService,
        [FromServices] MarketDataService marketData)
    {
        ticker = ticker.ToUpper().Trim();

        if (!await db.BistSymbols.AnyAsync(s => s.Ticker == ticker))
            await symbolService.AddSymbolAsync(ticker, null, null, null, marketData);

        var cfg = await db.BotConfigs.FirstOrDefaultAsync(c => c.UserId == UserId)
            ?? throw new AppException("Bot config not found", 404);

        if (!cfg.Watchlist.Contains(ticker))
        {
            cfg.Watchlist = [.. cfg.Watchlist, ticker];
            cfg.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return Ok(ToDto(cfg));
    }

    /// Remove a ticker from the watchlist immediately.
    [HttpDelete("watchlist/{ticker}")]
    public async Task<IActionResult> RemoveFromWatchlist(string ticker)
    {
        ticker = ticker.ToUpper().Trim();
        var cfg = await db.BotConfigs.FirstOrDefaultAsync(c => c.UserId == UserId)
            ?? throw new AppException("Bot config not found", 404);

        cfg.Watchlist = cfg.Watchlist.Where(t => t != ticker).ToArray();
        cfg.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(cfg));
    }

    private static BotConfigDto ToDto(BotConfig c) => new(
        c.Id, c.UserId, c.Enabled, c.ConfidenceThreshold,
        c.MaxOrderSizeTry, c.DailyLossLimitTry, c.TickIntervalMs, c.Watchlist, c.UpdatedAt);
}
