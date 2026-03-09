using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.DTOs;
using TradingBot.Indicators;

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

    private static SymbolDto ToDto(Models.BistSymbol s) => new(s.Id, s.Ticker, s.Name, s.Sector, s.LastPrice, s.UpdatedAt);
}
