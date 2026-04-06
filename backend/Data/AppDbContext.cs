using Microsoft.EntityFrameworkCore;
using TradingBot.Models;

namespace TradingBot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BistSymbol> BistSymbols => Set<BistSymbol>();
    public DbSet<PriceCandle> PriceCandles => Set<PriceCandle>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<BotConfig> BotConfigs => Set<BotConfig>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique indexes
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<BistSymbol>().HasIndex(s => s.Ticker).IsUnique();
        modelBuilder.Entity<Position>().HasIndex(p => new { p.UserId, p.SymbolId }).IsUnique();
        modelBuilder.Entity<BotConfig>().HasIndex(b => b.UserId).IsUnique();

        // Composite index for candle queries
        modelBuilder.Entity<PriceCandle>().HasIndex(c => new { c.SymbolId, c.Timestamp });

        // Store enums as strings
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
        modelBuilder.Entity<BistSymbol>().Property(s => s.Type).HasConversion<string>();
        modelBuilder.Entity<Order>().Property(o => o.Type).HasConversion<string>();
        modelBuilder.Entity<Order>().Property(o => o.Status).HasConversion<string>();
        modelBuilder.Entity<Notification>().Property(n => n.Channel).HasConversion<string>();
        modelBuilder.Entity<Notification>().Property(n => n.Status).HasConversion<string>();

        // Cascade deletes
        modelBuilder.Entity<PriceCandle>()
            .HasOne(c => c.Symbol)
            .WithMany(s => s.Candles)
            .HasForeignKey(c => c.SymbolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BotConfig>()
            .HasOne(b => b.User)
            .WithOne(u => u.BotConfig)
            .HasForeignKey<BotConfig>(b => b.UserId);
    }
}
