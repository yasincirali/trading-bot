using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Hubs;
using TradingBot.Indicators;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Bot;

public class TradingEngine(
    IServiceScopeFactory scopeFactory,
    IHubContext<TradingHub> hubContext,
    ILogger<TradingEngine> logger) : BackgroundService
{
    private readonly Dictionary<Guid, CancellationTokenSource> _activeBots = new();

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
                var marketData = scope.ServiceProvider.GetRequiredService<MarketDataService>();

                var cfg = await db.BotConfigs.FirstOrDefaultAsync(c => c.UserId == userId, token);
                if (cfg == null || !cfg.Enabled)
                {
                    StopBot(userId);
                    return;
                }

                // Query symbols first (need Type for correct Yahoo ticker mapping)
                var symbols = await db.BistSymbols
                    .Where(s => cfg.Watchlist.Contains(s.Ticker))
                    .ToListAsync(token);

                // Batch-fetch live prices for the entire watchlist (single HTTP call)
                // Missing tickers (API failure) will fall back to last known DB price
                var livePrices = await marketData.GetCurrentPricesAsync(symbols);
                foreach (var sym in symbols)
                    if (!livePrices.ContainsKey(sym.Ticker) && sym.LastPrice > 0)
                        livePrices[sym.Ticker] = sym.LastPrice;

                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                await RunTickAsync(userId, cfg, db, orderExecutor, notificationService, marketData, livePrices, token);
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

    private async Task RunTickAsync(
        Guid userId, BotConfig cfg, AppDbContext db,
        OrderExecutor executor, NotificationService notifications,
        MarketDataService marketData, Dictionary<string, double> livePrices,
        CancellationToken token)
    {
        foreach (var ticker in cfg.Watchlist)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                // Store the live price as a new candle (replaces simulation)
                if (livePrices.TryGetValue(ticker, out var livePrice))
                    await StoreLivePriceAsync(ticker, livePrice, db);

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
                {
                    int estimatedQty = (int)(cfg.MaxOrderSizeTry / Math.Max(currentPrice, 0.01));

                    if (cfg.NotifyOnSignal)
                    {
                        bool paperTrade = executor.IsPaperTrade();
                        _ = notifications.SendTradeSignalAsync(userId, new TradeSignalInfo(ticker, signal, currentPrice, estimatedQty, paperTrade));
                    }

                    await executor.ExecuteAsync(userId, ticker, signal, cfg.MaxOrderSizeTry, cfg.DailyLossLimitTry, cfg.NotifyOnOrder ? notifications : null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing {Ticker}", ticker);
            }
        }
    }

    private static async Task StoreLivePriceAsync(string ticker, double price, AppDbContext db)
    {
        var symbol = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == ticker);
        if (symbol == null || price <= 0) return;

        // Always keep LastPrice current so the UI always shows something
        symbol.LastPrice = price;
        symbol.UpdatedAt = DateTime.UtcNow;

        // Only add a new candle if the price changed or it has been > 1 hour
        // (avoids thousands of identical candles when market is closed)
        var lastCandle = await db.PriceCandles
            .Where(c => c.SymbolId == symbol.Id)
            .OrderByDescending(c => c.Timestamp)
            .FirstOrDefaultAsync();

        bool priceChanged = lastCandle == null || Math.Abs(lastCandle.Close - price) > 0.001;
        bool stale = lastCandle == null || DateTime.UtcNow - lastCandle.Timestamp > TimeSpan.FromHours(1);

        if (priceChanged || stale)
        {
            db.PriceCandles.Add(new PriceCandle
            {
                SymbolId = symbol.Id,
                Open = lastCandle?.Close ?? price,
                High = Math.Max(lastCandle?.Close ?? price, price),
                Low = Math.Min(lastCandle?.Close ?? price, price),
                Close = price,
                Volume = 0,
                Timestamp = DateTime.UtcNow,
            });
        }

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
