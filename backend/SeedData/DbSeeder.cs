using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.SeedData;

public static class DbSeeder
{
    private static readonly (string Ticker, string Name, string Sector, double BasePrice, SymbolType Type)[] Symbols =
    [
        // BIST Stocks
        ("THYAO", "Türk Hava Yolları",   "Havacılık",  280.5, SymbolType.STOCK),
        ("EREGL", "Ereğli Demir Çelik",  "Metal",       45.2, SymbolType.STOCK),
        ("GARAN", "Garanti BBVA Bankası", "Bankacılık", 105.8, SymbolType.STOCK),
        ("ASELS", "Aselsan",              "Savunma",     95.4, SymbolType.STOCK),
        ("SISE",  "Şişe ve Cam",          "Cam",         52.3, SymbolType.STOCK),
        ("KCHOL", "Koç Holding",          "Holding",    178.6, SymbolType.STOCK),
        ("BIMAS", "BİM Mağazalar",        "Perakende",  495.0, SymbolType.STOCK),
        ("SAHOL", "Sabancı Holding",      "Holding",     88.9, SymbolType.STOCK),
        ("TUPRS", "Tüpraş",               "Petrol",     420.0, SymbolType.STOCK),
        ("AKBNK", "Akbank",               "Bankacılık",  62.7, SymbolType.STOCK),
        // Forex (TRY pairs)
        ("USDTRY", "ABD Doları / TL",           "Döviz",  32.50, SymbolType.FOREX),
        ("EURTRY", "Euro / TL",                 "Döviz",  35.20, SymbolType.FOREX),
        ("GBPTRY", "İngiliz Sterlini / TL",     "Döviz",  41.10, SymbolType.FOREX),
        ("JPYTRY", "Japon Yeni / TL",           "Döviz",   0.22, SymbolType.FOREX),
        // Commodities
        ("ALTIN",  "Altın (Ons/USD)",   "Emtia",  2300.0, SymbolType.COMMODITY),
        ("GUMUS",  "Gümüş (Ons/USD)",   "Emtia",    27.5, SymbolType.COMMODITY),
        ("PETROL", "Ham Petrol (USD)",   "Emtia",    80.0, SymbolType.COMMODITY),
        ("BRENT",  "Brent Petrol (USD)", "Emtia",    84.0, SymbolType.COMMODITY),
    ];

    public static async Task SeedAsync(AppDbContext db, MarketDataService? marketData = null)
    {
        // Admin user
        if (!db.Users.Any(u => u.Email == "admin@tradingbot.tr"))
        {
            var admin = new User
            {
                Email = "admin@tradingbot.tr",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Name = "Admin User",
                Phone = "+905551234567",
                Role = UserRole.ADMIN,
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            db.BotConfigs.Add(new BotConfig
            {
                UserId = admin.Id,
                ConfidenceThreshold = 0.65,
                MaxOrderSizeTry = 10000,
                DailyLossLimitTry = 5000,
                TickIntervalMs = 5000,
                Watchlist = ["THYAO", "GARAN", "AKBNK", "EREGL", "ASELS"],
            });
            await db.SaveChangesAsync();
            Console.WriteLine("[Seed] Admin user created: admin@tradingbot.tr / Admin123!");
        }

        // All symbols + candles
        var rng = new Random(42); // deterministic seed
        foreach (var (ticker, name, sector, basePrice, symType) in Symbols)
        {
            BistSymbol symbol;
            if (!db.BistSymbols.Any(s => s.Ticker == ticker))
            {
                symbol = new BistSymbol { Ticker = ticker, Name = name, Sector = sector, Type = symType };
                db.BistSymbols.Add(symbol);
                await db.SaveChangesAsync();
            }
            else
            {
                symbol = db.BistSymbols.First(s => s.Ticker == ticker);
            }

            if (db.PriceCandles.Any(c => c.SymbolId == symbol.Id)) continue;

            IReadOnlyList<PriceCandle> candles;

            // Try real Yahoo Finance data first, fall back to synthetic
            if (marketData != null)
            {
                var liveData = await marketData.GetHistoricalAsync(ticker, 200, symType);
                if (liveData.Count >= 30)
                {
                    candles = liveData.Select(d => new PriceCandle
                    {
                        SymbolId = symbol.Id,
                        Open = d.Open, High = d.High, Low = d.Low, Close = d.Close,
                        Volume = d.Volume, Timestamp = d.Timestamp,
                    }).ToList();
                    Console.WriteLine($"[Seed] {ticker}: {candles.Count} real candles from Yahoo Finance, last={candles[^1].Close:F4}");
                }
                else
                {
                    Console.WriteLine($"[Seed] {ticker}: Yahoo Finance returned {liveData.Count} candles, falling back to synthetic");
                    candles = GenerateCandles(symbol.Id, basePrice, 200, rng);
                }
            }
            else
            {
                candles = GenerateCandles(symbol.Id, basePrice, 200, rng);
                Console.WriteLine($"[Seed] {ticker}: {candles.Count} synthetic candles, last={candles[^1].Close:F4}");
            }

            db.PriceCandles.AddRange(candles);
            symbol.LastPrice = candles[^1].Close;
            symbol.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    private static IReadOnlyList<PriceCandle> GenerateCandles(Guid symbolId, double basePrice, int days, Random rng)
    {
        var candles = new PriceCandle[days + 1];
        double price = basePrice;
        var now = DateTime.UtcNow;

        for (int i = 0; i <= days; i++)
        {
            var date = now.AddDays(-(days - i)).Date.AddHours(10);
            double change = (rng.NextDouble() - 0.48) * price * 0.025;
            price = Math.Max(price + change, 1.0);

            double volatility = price * 0.015;
            double open = price;
            double high = open + rng.NextDouble() * volatility;
            double low = Math.Max(open - rng.NextDouble() * volatility, 0.01);
            double close = low + rng.NextDouble() * (high - low);

            candles[i] = new PriceCandle
            {
                SymbolId = symbolId,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = rng.Next(100_000, 1_000_000),
                Timestamp = date,
            };
            price = close;
        }

        return candles;
    }
}
