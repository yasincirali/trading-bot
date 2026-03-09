using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.DTOs;
using TradingBot.Models;

namespace TradingBot.Services;

public class UserService(AppDbContext db, JwtService jwt, IConfiguration config)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            throw new AppException("Email already registered", 409);

        if (req.Password.Length < 6)
            throw new AppException("Password must be at least 6 characters", 400);

        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Name = req.Name,
            Phone = req.Phone,
        };
        db.Users.Add(user);

        db.BotConfigs.Add(new BotConfig
        {
            UserId = user.Id,
            ConfidenceThreshold = config.GetValue<double>("Trading:ConfidenceThreshold", 0.65),
            MaxOrderSizeTry = config.GetValue<double>("Trading:MaxOrderSizeTry", 10000),
            DailyLossLimitTry = config.GetValue<double>("Trading:DailyLossLimitTry", 5000),
            TickIntervalMs = config.GetValue<int>("Trading:TickIntervalMs", 5000),
            Watchlist = ["THYAO", "GARAN", "AKBNK"],
        });

        await db.SaveChangesAsync();
        var (access, refresh) = jwt.GenerateTokens(user);
        return new AuthResponse(ToDto(user), new TokensDto(access, refresh));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new AppException("Invalid email or password", 401);

        var (access, refresh) = jwt.GenerateTokens(user);
        return new AuthResponse(ToDto(user), new TokensDto(access, refresh));
    }

    public async Task<UserDto> GetByIdAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new AppException("User not found", 404);
        return ToDto(user);
    }

    public async Task<TokensDto> RefreshAsync(string refreshToken)
    {
        var principal = jwt.ValidateToken(refreshToken)
            ?? throw new AppException("Invalid refresh token", 401);

        var userIdStr = principal.FindFirst("userId")?.Value
            ?? throw new AppException("Invalid token claims", 401);

        var user = await db.Users.FindAsync(Guid.Parse(userIdStr))
            ?? throw new AppException("User not found", 404);

        var (access, refresh) = jwt.GenerateTokens(user);
        return new TokensDto(access, refresh);
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.Name, u.Phone, u.Role, u.CreatedAt);
}

public class AppException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
