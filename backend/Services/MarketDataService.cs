using System.Text.Json;

namespace TradingBot.Services;

public class MarketDataService(IHttpClientFactory httpFactory, ILogger<MarketDataService> logger)
{
    private const string Base = "https://query1.finance.yahoo.com";

    // Per-symbol price cache (30s TTL) — avoids hammering Yahoo Finance on every tick
    private readonly Dictionary<string, (double Price, DateTime At)> _cache = [];
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);
    private readonly Lock _cacheLock = new();

    public record CandleData(DateTime Timestamp, double Open, double High, double Low, double Close, double Volume);

    // ── Batch current prices ──────────────────────────────────────────────────

    public async Task<Dictionary<string, double>> GetCurrentPricesAsync(IEnumerable<string> bistTickers)
    {
        var tickers = bistTickers.ToList();
        var result = new Dictionary<string, double>();
        var uncached = new List<string>();
        var now = DateTime.UtcNow;

        lock (_cacheLock)
        {
            foreach (var t in tickers)
            {
                if (_cache.TryGetValue(t, out var c) && now - c.At < _cacheTtl)
                    result[t] = c.Price;
                else
                    uncached.Add(t);
            }
        }

        if (uncached.Count == 0) return result;

        try
        {
            var symbols = string.Join(",", uncached.Select(t => $"{t}.IS"));
            using var client = CreateClient();
            var json = await client.GetStringAsync($"{Base}/v7/finance/quote?symbols={Uri.EscapeDataString(symbols)}");
            using var doc = JsonDocument.Parse(json);

            var quotes = doc.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result")
                .EnumerateArray();

            lock (_cacheLock)
            {
                foreach (var q in quotes)
                {
                    var sym = q.GetProperty("symbol").GetString() ?? "";
                    var bistTicker = sym.Replace(".IS", "");
                    if (!q.TryGetProperty("regularMarketPrice", out var priceEl)) continue;
                    var price = priceEl.GetDouble();
                    if (price <= 0) continue;
                    result[bistTicker] = price;
                    _cache[bistTicker] = (price, now);
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

    public async Task<List<CandleData>> GetHistoricalAsync(string bistTicker, int days = 200)
    {
        var result = new List<CandleData>();
        try
        {
            var symbol = $"{bistTicker}.IS";
            var range = days <= 252 ? "1y" : "2y";
            using var client = CreateClient();
            var json = await client.GetStringAsync($"{Base}/v8/finance/chart/{symbol}?interval=1d&range={range}");
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

            // Keep only the last `days` candles
            if (result.Count > days)
                result = result.GetRange(result.Count - days, days);

            logger.LogInformation("[MarketData] {Ticker}: fetched {Count} historical candles from Yahoo Finance", bistTicker, result.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MarketData] Historical fetch failed for {Ticker}", bistTicker);
        }

        return result;
    }

    // ── Symbol metadata ───────────────────────────────────────────────────────

    public record SymbolInfo(string Name, string Sector, string Type, double Price);

    public async Task<SymbolInfo?> GetSymbolInfoAsync(string bistTicker)
    {
        try
        {
            var symbol = $"{bistTicker}.IS";
            using var client = CreateClient();
            var json = await client.GetStringAsync($"{Base}/v7/finance/quote?symbols={Uri.EscapeDataString(symbol)}");
            using var doc = JsonDocument.Parse(json);

            var results = doc.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result");

            if (results.GetArrayLength() == 0) return null;
            var q = results[0];

            var name = q.TryGetProperty("shortName", out var sn) ? sn.GetString()
                     : q.TryGetProperty("longName", out var ln) ? ln.GetString()
                     : bistTicker;

            var sector = q.TryGetProperty("sector", out var sec) ? sec.GetString() ?? "Diğer" : "Diğer";

            var quoteType = q.TryGetProperty("quoteType", out var qt) ? qt.GetString() ?? "" : "";
            var type = quoteType is "ETF" or "MUTUALFUND" ? "FUND" : "STOCK";

            var price = q.TryGetProperty("regularMarketPrice", out var pr) ? pr.GetDouble() : 0.0;

            logger.LogInformation("[MarketData] Symbol info fetched: {Ticker} — {Name} ({Type})", bistTicker, name, type);
            return new SymbolInfo(name ?? bistTicker, sector, type, price);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MarketData] Symbol info fetch failed for {Ticker}", bistTicker);
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
