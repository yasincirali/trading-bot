using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Hubs;
using TradingBot.Indicators;
using TradingBot.Models;

namespace TradingBot.Bot;

public class TradingEngine(
    IServiceScopeFactory scopeFactory,
    IHubContext<TradingHub> hubContext,
    ILogger<TradingEngine> logger) : BackgroundService
{
    private readonly Dictionary<Guid, CancellationTokenSource> _activeBots = new();
    private readonly Random _rng = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Resume bots that were enabled before restart
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var enabled = await db.BotConfigs.Where(c => c.Enabled).ToListAsync(stoppingToken);
        foreach (var cfg in enabled)
        {
            StartBot(cfg.UserId);
            logger.LogInformation("Resumed bot for user {UserId}", cfg.UserId);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public void StartBot(Guid userId)
    {
        if (_activeBots.ContainsKey(userId)) return;
        var cts = new CancellationTokenSource();
        _activeBots[userId] = cts;
        _ = RunBotLoopAsync(userId, cts.Token);
        logger.LogInformation("Bot started for user {UserId}", userId);
    }

    public void StopBot(Guid userId)
    {
        if (!_activeBots.TryGetValue(userId, out var cts)) return;
        cts.Cancel();
        _activeBots.Remove(userId);
        logger.LogInformation("Bot stopped for user {UserId}", userId);
    }

    public bool IsBotRunning(Guid userId) => _activeBots.ContainsKey(userId);

    private async Task RunBotLoopAsync(Guid userId, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var orderExecutor = scope.ServiceProvider.GetRequiredService<OrderExecutor>();

                var cfg = await db.BotConfigs.FirstOrDefaultAsync(c => c.UserId == userId, token);
                if (cfg == null || !cfg.Enabled)
                {
                    StopBot(userId);
                    return;
                }

                await RunTickAsync(userId, cfg, db, orderExecutor, token);
                await EmitPriceUpdatesAsync(userId, cfg.Watchlist, db);
                await Task.Delay(cfg.TickIntervalMs, token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tick error for user {UserId}", userId);
                try { await Task.Delay(5000, token); } catch { break; }
            }
        }
    }

    private async Task RunTickAsync(Guid userId, BotConfig cfg, AppDbContext db, OrderExecutor executor, CancellationToken token)
    {
        foreach (var ticker in cfg.Watchlist)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var candles = await db.PriceCandles
                    .Where(c => c.Symbol.Ticker == ticker)
                    .OrderByDescending(c => c.Timestamp)
                    .Take(200)
                    .ToListAsync(token);

                if (candles.Count < 30) continue;

                var prices = candles.Select(c => c.Close).Reverse().ToArray();
                var signal = SignalAggregator.Aggregate(prices);
                var currentPrice = prices[^1];

                await hubContext.Clients.Group($"user:{userId}").SendAsync("BotTick", new
                {
                    ticker,
                    signal = MapSignal(signal),
                    price = currentPrice,
                    timestamp = DateTime.UtcNow,
                }, token);

                if (signal.Confidence >= cfg.ConfidenceThreshold && signal.Signal != SignalType.NEUTRAL)
                    await executor.ExecuteAsync(userId, ticker, signal, cfg.MaxOrderSizeTry, cfg.DailyLossLimitTry);

                await SimulatePriceMoveAsync(ticker, currentPrice, db);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing {Ticker}", ticker);
            }
        }
    }

    private async Task SimulatePriceMoveAsync(string ticker, double currentPrice, AppDbContext db)
    {
        double change = (_rng.NextDouble() - 0.5) * currentPrice * 0.003;
        double newPrice = Math.Max(currentPrice + change, 0.01);

        var symbol = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == ticker);
        if (symbol == null) return;

        symbol.LastPrice = newPrice;
        symbol.UpdatedAt = DateTime.UtcNow;

        db.PriceCandles.Add(new PriceCandle
        {
            SymbolId = symbol.Id,
            Open = currentPrice,
            High = Math.Max(currentPrice, newPrice) * (1 + _rng.NextDouble() * 0.002),
            Low = Math.Min(currentPrice, newPrice) * (1 - _rng.NextDouble() * 0.002),
            Close = newPrice,
            Volume = _rng.Next(10000, 60000),
            Timestamp = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
    }

    private async Task EmitPriceUpdatesAsync(Guid userId, string[] watchlist, AppDbContext db)
    {
        var symbols = await db.BistSymbols
            .Where(s => watchlist.Contains(s.Ticker))
            .ToListAsync();

        foreach (var sym in symbols)
        {
            var latest = await db.PriceCandles
                .Where(c => c.SymbolId == sym.Id)
                .OrderByDescending(c => c.Timestamp)
                .Take(2)
                .ToListAsync();

            double current = latest.FirstOrDefault()?.Close ?? sym.LastPrice;
            double prev = latest.Count > 1 ? latest[1].Close : current;
            double change = current - prev;
            double changePct = prev != 0 ? change / prev * 100 : 0;

            await hubContext.Clients.Group($"user:{userId}").SendAsync("PriceUpdate", new
            {
                ticker = sym.Ticker,
                price = current,
                change,
                changePercent = changePct,
                timestamp = DateTime.UtcNow,
            });
        }
    }

    private static object MapSignal(AggregatedSignal s) => new
    {
        signal = s.Signal.ToString(),
        strength = s.Strength,
        confidence = s.Confidence,
        components = new
        {
            rsi = new { signal = s.Rsi.Signal.ToString(), strength = s.Rsi.Strength, value = s.Rsi.Value },
            macd = new { signal = s.Macd.Signal.ToString(), strength = s.Macd.Strength, value = s.Macd.Value },
            bollinger = new { signal = s.Bollinger.Signal.ToString(), strength = s.Bollinger.Strength, value = s.Bollinger.Value },
            ema = new { signal = s.Ema.Signal.ToString(), strength = s.Ema.Strength, value = s.Ema.Value },
        }
    };
}
