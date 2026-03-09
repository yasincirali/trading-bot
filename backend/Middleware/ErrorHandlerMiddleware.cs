using System.Text.Json;
using TradingBot.Services;

namespace TradingBot.Middleware;

public class ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (AppException ex)
        {
            logger.LogWarning("[{Method} {Path}] {Status}: {Message}", ctx.Request.Method, ctx.Request.Path, ex.StatusCode, ex.Message);
            ctx.Response.StatusCode = ex.StatusCode;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Method} {Path}] Unhandled exception", ctx.Request.Method, ctx.Request.Path);
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            var msg = ctx.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                ? ex.ToString() : "Internal server error";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = msg }));
        }
    }
}
