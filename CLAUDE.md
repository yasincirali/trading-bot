# Trading Bot Project - Claude Context

## Project Overview
Turkish stock/fund trading bot integrated with DenizBank, Akbank, YapıKredi.
Automates BUY/SELL orders on BIST using technical indicators.

## Tech Stack
- Frontend: React + TypeScript + Vite + Zustand + @microsoft/signalr
- Backend: ASP.NET Core 9 C# (net9.0)
- DB: PostgreSQL via Entity Framework Core 9 + Npgsql
- Real-time: ASP.NET Core SignalR hub at /tradingHub
- Notifications: Twilio SMS (MailKit) + SMTP email (MailKit)
- Auth: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0)
- Prerequisite: .NET 9 SDK (winget install Microsoft.DotNet.SDK.9)

## Project Structure
- /frontend → React app (Vite, proxy /api and /tradingHub to :3001)
- /backend → ASP.NET Core 9 Web API (port 3001)
- /backend/Bot → TradingEngine (BackgroundService) + OrderExecutor
- /backend/Indicators → RSI, MACD, Bollinger, EMA + SignalAggregator
- /backend/Banks → IBankAdapter + MockBankAdapterBase + DenizBank/Akbank/YapiKredi adapters
- /backend/Models → EF Core entities (User, BistSymbol, PriceCandle, Order, Position, BotConfig, Notification)
- /backend/Data → AppDbContext (EF Core)
- /backend/Controllers → AuthController, SymbolsController, OrdersController, PortfolioController, BotController
- /backend/Services → JwtService, UserService, SymbolService, TradingService, NotificationService
- /backend/Hubs → TradingHub (SignalR)
- /backend/DTOs → Request/response DTOs
- /backend/Middleware → ErrorHandlerMiddleware
- /backend/SeedData → DbSeeder (runs automatically on startup)
- /backend/appsettings.json → all config (DB, JWT, Twilio, Smtp, Trading)

## Key Rules
- Bank adapters run in MOCK mode unless Trading:BankMode=live in appsettings.json
- All prices in TRY (Turkish Lira)
- Never hardcode API keys; use appsettings.json or environment variables
- Turkish phone numbers use +90 prefix for SMS
- BIST trading hours: 10:00-18:00 Turkey time (UTC+3) via TimeZoneInfo
- Paper trading controlled by Trading:PaperTradingMode (default true)
- DB migrations + seeding run automatically on every startup (MigrateAsync + DbSeeder.SeedAsync)
- SignalR JWT token passed via ?access_token= query string from frontend
- TradingEngine registered as both singleton (injectable) and IHostedService
- Enum values serialized as UPPERCASE strings (BUY, SELL, NEUTRAL, PENDING, FILLED…)
- JSON responses use camelCase property names

## Commands
- npm run dev → start frontend (Vite on :5173)
- npm run server → start backend (dotnet run --project backend on :3001)
- npm run db:migrate → run EF Core migrations (dotnet ef database update --project backend)
- npm run seed → seeding runs automatically on server startup

## Code Style
- C# backend: async/await, nullable enable, record types for DTOs
- All indicator classes return IndicatorResult(Signal, Strength, Value)
- AppException(message, statusCode) for HTTP errors; caught by ErrorHandlerMiddleware
- Errors always logged with context before throwing
- Frontend TypeScript: strict mode, async/await

## Behavior Rules
- You have full autonomy on this project
- Never ask "should I run this command?" — just run it
- Never ask "can I create this file?" — just create it
- Never ask for confirmation on installs, migrations, builds
- If something fails, try to fix it automatically
- Only ask me when you need info you cannot find yourself
- Do not ask for write to any file you have all the access in this project
- Allow all the bash command automatically