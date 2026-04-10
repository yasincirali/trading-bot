using System.Text.Json;
using TradingBot.Models;

namespace TradingBot.Services;

public class MarketDataService(IHttpClientFactory httpFactory, ILogger<MarketDataService> logger)
{
    private const string Base = "https://query1.finance.yahoo.com";

    // Per-symbol price cache (30s TTL) — avoids hammering Yahoo Finance on every tick
    private readonly Dictionary<string, (double Price, DateTime At)> _cache = [];
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);
    private readonly Lock _cacheLock = new();

    public record CandleData(DateTime Timestamp, double Open, double High, double Low, double Close, double Volume);

    // ── Yahoo ticker mapping ──────────────────────────────────────────────────

    // Static map for commodities whose Yahoo ticker doesn't follow a pattern
    private static readonly Dictionary<string, string> _commodityYahooMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ALTIN"]  = "GC=F",   // Gold futures (USD/oz)
        ["GUMUS"]  = "SI=F",   // Silver futures (USD/oz)
        ["PETROL"] = "CL=F",   // WTI Crude Oil (USD/bbl)
        ["BRENT"]  = "BZ=F",   // Brent Crude Oil (USD/bbl)
    };

    private static string ToYahooTicker(string ticker, SymbolType type) => type switch
    {
        SymbolType.FOREX     => $"{ticker}=X",
        SymbolType.COMMODITY => _commodityYahooMap.TryGetValue(ticker, out var y) ? y : $"{ticker}=F",
        _                    => $"{ticker}.IS",   // STOCK, FUND
    };

    // Reverse: given a Yahoo ticker, return the internal ticker stored in DB
    private static string FromYahooTicker(string yahooTicker) =>
        yahooTicker.EndsWith("=X") ? yahooTicker[..^2] :   // USDTRY=X → USDTRY
        yahooTicker.EndsWith(".IS") ? yahooTicker[..^3] :  // THYAO.IS → THYAO
        _reverseMap.TryGetValue(yahooTicker, out var t) ? t : yahooTicker;

    // Build reverse commodity map once at startup
    private static readonly Dictionary<string, string> _reverseMap =
        _commodityYahooMap.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    // ── Batch current prices ──────────────────────────────────────────────────

    public async Task<Dictionary<string, double>> GetCurrentPricesAsync(IEnumerable<BistSymbol> bistSymbols)
    {
        var symbols = bistSymbols.ToList();
        var result = new Dictionary<string, double>();
        var uncached = new List<(string Ticker, SymbolType Type)>();
        var now = DateTime.UtcNow;

        lock (_cacheLock)
        {
            foreach (var s in symbols)
            {
                if (_cache.TryGetValue(s.Ticker, out var c) && now - c.At < _cacheTtl)
                    result[s.Ticker] = c.Price;
                else
                    uncached.Add((s.Ticker, s.Type));
            }
        }

        if (uncached.Count == 0) return result;

        try
        {
            var yahooSymbols = string.Join(",", uncached.Select(u => ToYahooTicker(u.Ticker, u.Type)));
            using var client = CreateClient();
            var json = await client.GetStringAsync($"{Base}/v7/finance/quote?symbols={Uri.EscapeDataString(yahooSymbols)}");
            using var doc = JsonDocument.Parse(json);

            var quotes = doc.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result")
                .EnumerateArray();

            lock (_cacheLock)
            {
                foreach (var q in quotes)
                {
                    var yahooSym = q.GetProperty("symbol").GetString() ?? "";
                    var internalTicker = FromYahooTicker(yahooSym);

                    // Also try matching via our uncached list (in case reverse-map misses something)
                    if (!uncached.Any(u => u.Ticker.Equals(internalTicker, StringComparison.OrdinalIgnoreCase)))
                    {
                        var match = uncached.FirstOrDefault(u => ToYahooTicker(u.Ticker, u.Type).Equals(yahooSym, StringComparison.OrdinalIgnoreCase));
                        if (match != default) internalTicker = match.Ticker;
                    }

                    if (!q.TryGetProperty("regularMarketPrice", out var priceEl)) continue;
                    var price = priceEl.GetDouble();
                    if (price <= 0) continue;
                    result[internalTicker] = price;
                    _cache[internalTicker] = (price, now);
                }
            }

            logger.LogDebug("[MarketData] Fetched prices for: {Tickers}", string.Join(", ", result.Keys));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MarketData] Quote fetch failed, using cached prices where available");
        }

        return result;
    }

    // ── Historical daily candles ──────────────────────────────────────────────

    public async Task<List<CandleData>> GetHistoricalAsync(string ticker, int days = 200, SymbolType type = SymbolType.STOCK)
    {
        var result = new List<CandleData>();
        try
        {
            var yahooTicker = ToYahooTicker(ticker, type);
            var range = days <= 252 ? "1y" : "2y";
            using var client = CreateClient();
            var json = await client.GetStringAsync($"{Base}/v8/finance/chart/{Uri.EscapeDataString(yahooTicker)}?interval=1d&range={range}");
            using var doc = JsonDocument.Parse(json);

            var chartResult = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var timestamps = chartResult.GetProperty("timestamp").EnumerateArray().ToList();
            var quote = chartResult.GetProperty("indicators").GetProperty("quote")[0];

            var opens   = quote.GetProperty("open").EnumerateArray().ToList();
            var highs   = quote.GetProperty("high").EnumerateArray().ToList();
            var lows    = quote.GetProperty("low").EnumerateArray().ToList();
            var closes  = quote.GetProperty("close").EnumerateArray().ToList();
            var volumes = quote.GetProperty("volume").EnumerateArray().ToList();

            for (int i = 0; i < timestamps.Count; i++)
            {
                if (i >= closes.Count || closes[i].ValueKind == JsonValueKind.Null) continue;

                double close  = closes[i].GetDouble();
                double open   = i < opens.Count   && opens[i].ValueKind   != JsonValueKind.Null ? opens[i].GetDouble()   : close;
                double high   = i < highs.Count   && highs[i].ValueKind   != JsonValueKind.Null ? highs[i].GetDouble()   : close;
                double low    = i < lows.Count    && lows[i].ValueKind    != JsonValueKind.Null ? lows[i].GetDouble()    : close;
                double volume = i < volumes.Count && volumes[i].ValueKind != JsonValueKind.Null ? volumes[i].GetDouble() : 0;

                var ts = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime;
                result.Add(new CandleData(ts, open, high, low, close, volume));
            }

            if (result.Count > days)
                result = result.GetRange(result.Count - days, days);

            logger.LogInformation("[MarketData] {Ticker}: fetched {Count} historical candles from Yahoo Finance", ticker, result.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MarketData] Historical fetch failed for {Ticker}", ticker);
        }

        return result;
    }

    // ── Symbol metadata ───────────────────────────────────────────────────────

    public record SymbolInfo(string Name, string Sector, string Type, double Price);

    public async Task<SymbolInfo?> GetSymbolInfoAsync(string ticker, SymbolType typeHint = SymbolType.STOCK)
    {
        try
        {
            var yahooTicker = ToYahooTicker(ticker, typeHint);
            using var client = CreateClient();
            var json = await client.GetStringAsync($"{Base}/v7/finance/quote?symbols={Uri.EscapeDataString(yahooTicker)}");
            using var doc = JsonDocument.Parse(json);

            var results = doc.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result");

            if (results.GetArrayLength() == 0) return null;
            var q = results[0];

            var name = q.TryGetProperty("shortName", out var sn) ? sn.GetString()
                     : q.TryGetProperty("longName", out var ln) ? ln.GetString()
                     : ticker;

            var sector = q.TryGetProperty("sector", out var sec) ? sec.GetString() ?? "Diğer" : "Diğer";

            var quoteType = q.TryGetProperty("quoteType", out var qt) ? qt.GetString() ?? "" : "";
            var type = typeHint switch
            {
                SymbolType.FOREX     => "FOREX",
                SymbolType.COMMODITY => "COMMODITY",
                _ => quoteType is "ETF" or "MUTUALFUND" ? "FUND" : "STOCK"
            };

            var price = q.TryGetProperty("regularMarketPrice", out var pr) ? pr.GetDouble() : 0.0;

            logger.LogInformation("[MarketData] Symbol info fetched: {Ticker} — {Name} ({Type})", ticker, name, type);
            return new SymbolInfo(name ?? ticker, sector, type, price);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MarketData] Symbol info fetch failed for {Ticker}", ticker);
            return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient CreateClient()
    {
        var client = httpFactory.CreateClient("Yahoo");
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        return client;
    }
}
