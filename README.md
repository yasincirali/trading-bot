# Turkish Trading Bot (BIST)

Automated stock/fund trading bot for the **Istanbul Stock Exchange (BIST)** integrated with DenizBank, Akbank, and YapıKredi investment platforms. Generates BUY/SELL signals from RSI, MACD, Bollinger Bands, and EMA indicators.

> **Default mode:** Paper trading (no real orders placed). Runs in mock bank mode unless configured otherwise.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18 + TypeScript + Vite + Zustand |
| Backend | ASP.NET Core 9 Web API (C#) |
| Database | PostgreSQL 16 + EF Core 9 (Npgsql) |
| Real-time | SignalR (`/tradingHub`) |
| Auth | JWT Bearer (7-day access / 30-day refresh) |
| Notifications | Twilio SMS + SMTP email (MailKit) |

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) — `winget install Microsoft.DotNet.SDK.9`
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)
- [GitHub CLI](https://cli.github.com/) (optional, for development)

---

## Quick Start

### 1. Start the database

```bash
docker compose up -d postgres
```

### 2. Configure local secrets

```bash
cp backend/appsettings.Local.json.example backend/appsettings.Local.json
```

Edit `backend/appsettings.Local.json` and set:
- **`Jwt:Secret`** — any random string, 32+ characters (required)
- **`ConnectionStrings:Default`** — leave as-is if using docker compose postgres
- **Twilio / SMTP** — optional, leave blank to disable notifications

### 3. Install frontend dependencies

```bash
npm install
```

### 4. Run the backend (auto-migrates DB + seeds data)

```bash
npm run server
# → http://localhost:3001  (Swagger: http://localhost:3001/swagger)
```

### 5. Run the frontend

```bash
npm run dev
# → http://localhost:5173
```

---

## Default Credentials

| Field | Value |
|-------|-------|
| Email | `admin@tradingbot.tr` |
| Password | `Admin123!` |

> Change these in production via `backend/SeedData/DbSeeder.cs`.

---

## Project Structure

```
turkish-trading-bot/
├── backend/                    # ASP.NET Core 9 Web API
│   ├── Bot/                    # TradingEngine (BackgroundService) + OrderExecutor
│   ├── Indicators/             # RSI, MACD, Bollinger, EMA + SignalAggregator
│   ├── Banks/                  # IBankAdapter + 3 mock adapters (DenizBank, Akbank, YKY)
│   ├── Controllers/            # Auth, Symbols, Orders, Portfolio, Bot
│   ├── Services/               # JWT, User, Symbol, Trading, Notification
│   ├── Hubs/                   # TradingHub (SignalR)
│   ├── Models/                 # EF Core entities
│   ├── Data/                   # AppDbContext
│   ├── SeedData/               # DbSeeder (idempotent, runs on startup)
│   ├── Migrations/             # EF Core migrations
│   ├── appsettings.json        # Default config (safe to commit — no real secrets)
│   └── appsettings.Local.json  # ← Your local secrets (gitignored, never commit)
├── frontend/                   # React + Vite app
│   └── src/
│       ├── stores/             # Zustand stores (auth, trading, etc.)
│       ├── components/         # UI components
│       └── socket/             # SignalR connection
├── docker-compose.yml          # PostgreSQL + pgAdmin + full stack
├── .env.example                # Environment variable template
└── package.json                # Root scripts
```

---

## Available Scripts

```bash
npm run dev          # Start frontend (Vite on :5173)
npm run server       # Start backend (dotnet run on :3001, auto-migrates)
npm run db:migrate   # Apply EF Core migrations manually
docker compose up -d # Start all services (postgres, pgadmin, backend, frontend)
```

---

## Configuration

All configuration lives in `backend/appsettings.json`. Sensitive values are overridden via `backend/appsettings.Local.json` (gitignored) or environment variables.

| Key | Description |
|-----|-------------|
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Jwt:Secret` | JWT signing key (32+ chars, keep secret) |
| `Twilio:*` | SMS notifications (optional) |
| `Smtp:*` | Email notifications (optional) |
| `Trading:PaperTradingMode` | `true` = no real orders (default) |
| `Trading:BankMode` | `mock` or `live` |

---

## Trading Logic

- **Signal Aggregation** — RSI 30% + MACD 30% + Bollinger 20% + EMA 20%
- **BIST Hours** — 10:00–18:00 Turkey time (UTC+3)
- **Confidence Threshold** — 0.65 (configurable)
- **Max Order Size** — 10,000 TRY (configurable)
- **Daily Loss Limit** — 5,000 TRY (configurable)

---

## Contributing

1. Fork & clone the repo
2. Follow **Quick Start** above
3. Create a feature branch: `git checkout -b feature/my-feature`
4. Commit your changes: `git commit -m "feat: add my feature"`
5. Push and open a Pull Request

**Important:** Never commit `appsettings.Local.json` or `.env`. These are gitignored for security.

---

## License

MIT
