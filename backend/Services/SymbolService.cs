using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.DTOs;
using TradingBot.Indicators;
using TradingBot.Models;

namespace TradingBot.Services;

public class SymbolService(AppDbContext db)
{
    public async Task<IEnumerable<SymbolDto>> GetAllAsync()
    {
        var symbols = await db.BistSymbols.OrderBy(s => s.Ticker).ToListAsync();
        return symbols.Select(ToDto);
    }

    public async Task<SymbolDto> GetByTickerAsync(string ticker)
    {
        var sym = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == ticker.ToUpper())
            ?? throw new AppException($"Symbol not found: {ticker}", 404);
        return ToDto(sym);
    }

    public async Task<IEnumerable<CandleDto>> GetCandlesAsync(string ticker, int limit = 200)
    {
        var sym = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == ticker.ToUpper())
            ?? throw new AppException($"Symbol not found: {ticker}", 404);

        var candles = await db.PriceCandles
            .Where(c => c.SymbolId == sym.Id)
            .OrderByDescending(c => c.Timestamp)
            .Take(limit)
            .ToListAsync();

        return candles
            .OrderBy(c => c.Timestamp)
            .Select(c => new CandleDto(c.Id, c.SymbolId, c.Open, c.High, c.Low, c.Close, c.Volume, c.Timestamp));
    }

    public async Task<AggregatedSignal> GetSignalsAsync(string ticker)
    {
        var candles = await GetCandlesAsync(ticker, 200);
        var prices = candles.Select(c => c.Close).ToArray();
        if (prices.Length < 30)
            return new AggregatedSignal(SignalType.NEUTRAL, 0, 0,
                new(SignalType.NEUTRAL, 0, null),
                new(SignalType.NEUTRAL, 0, null),
                new(SignalType.NEUTRAL, 0, null),
                new(SignalType.NEUTRAL, 0, null));

        return SignalAggregator.Aggregate(prices);
    }

    public async Task<SymbolDto> AddSymbolAsync(string ticker, string? name, string? sector, string? type, MarketDataService marketData)
    {
        ticker = ticker.ToUpper().Trim();

        // Return existing symbol if already registered
        var existing = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == ticker);
        if (existing != null) return ToDto(existing);

        // Determine type first so we can pass it to Yahoo Finance
        var symbolType = ParseType(type);

        // Fetch metadata from Yahoo Finance (pass type hint for correct ticker format)
        var info = await marketData.GetSymbolInfoAsync(ticker, symbolType);
        if (symbolType == SymbolType.STOCK && info?.Type != null)
            symbolType = ParseType(info.Type); // let Yahoo override STOCK→FUND if detected

        var symbol = new BistSymbol
        {
            Ticker = ticker,
            Name = name ?? info?.Name ?? ticker,
            Sector = sector ?? info?.Sector ?? "Diğer",
            Type = symbolType,
            LastPrice = info?.Price ?? 0,
        };
        db.BistSymbols.Add(symbol);
        await db.SaveChangesAsync();

        // Seed historical candles
        var candles = await marketData.GetHistoricalAsync(ticker, 200, symbolType);
        if (candles.Count >= 1)
        {
            db.PriceCandles.AddRange(candles.Select(d => new PriceCandle
            {
                SymbolId = symbol.Id,
                Open = d.Open, High = d.High, Low = d.Low, Close = d.Close,
                Volume = d.Volume, Timestamp = d.Timestamp,
            }));
            symbol.LastPrice = candles[^1].Close;
            await db.SaveChangesAsync();
        }

        return ToDto(symbol);
    }

    public async Task RemoveSymbolAsync(string ticker)
    {
        ticker = ticker.ToUpper();
        var symbol = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == ticker)
            ?? throw new AppException($"Symbol not found: {ticker}", 404);

        // Remove from all user watchlists
        var configs = await db.BotConfigs
            .Where(c => c.Watchlist.Contains(ticker))
            .ToListAsync();
        foreach (var cfg in configs)
            cfg.Watchlist = cfg.Watchlist.Where(t => t != ticker).ToArray();

        db.BistSymbols.Remove(symbol); // cascades to PriceCandles, Orders, Positions
        await db.SaveChangesAsync();
    }

    private static SymbolType ParseType(string? type) => type?.ToUpper() switch
    {
        "FUND"      => SymbolType.FUND,
        "FOREX"     => SymbolType.FOREX,
        "COMMODITY" => SymbolType.COMMODITY,
        _           => SymbolType.STOCK,
    };

    private static SymbolDto ToDto(BistSymbol s) =>
        new(s.Id, s.Ticker, s.Name, s.Sector, s.Type.ToString(), s.LastPrice, s.UpdatedAt);
}
