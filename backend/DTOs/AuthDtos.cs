using System.ComponentModel.DataAnnotations;
using TradingBot.Models;

namespace TradingBot.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Name,
    string? Phone
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record UserDto(Guid Id, string Email, string Name, string? Phone, UserRole Role, DateTime CreatedAt);

public record TokensDto(string AccessToken, string RefreshToken);

public record AuthResponse(UserDto User, TokensDto Tokens);

public record RefreshRequest(string RefreshToken);
