using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TradingBot.Hubs;

[Authorize]
public class TradingHub : Hub
{
    private readonly ILogger<TradingHub> _logger;

    public TradingHub(ILogger<TradingHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            _logger.LogInformation("[Hub] Connected: {UserId} ({ConnectionId})", userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        _logger.LogInformation("[Hub] Disconnected: {UserId}", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeSymbol(string ticker)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"symbol:{ticker}");
    }

    public async Task UnsubscribeSymbol(string ticker)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"symbol:{ticker}");
    }
}
