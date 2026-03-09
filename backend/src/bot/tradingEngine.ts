import { Server } from 'socket.io';
import { PrismaClient } from '@prisma/client';
import { aggregateSignals } from './signalAggregator';
import { executeOrder } from './orderExecutor';
import { BotTickEvent, PriceUpdateEvent } from '../types';

const prisma = new PrismaClient();

interface ActiveBot {
  userId: string;
  interval: NodeJS.Timeout;
  priceInterval: NodeJS.Timeout;
}

const activeBots = new Map<string, ActiveBot>();

export function startBot(userId: string, io: Server): void {
  if (activeBots.has(userId)) {
    console.log(`[TradingEngine] Bot already running for user ${userId}`);
    return;
  }

  console.log(`[TradingEngine] Starting bot for user ${userId}`);

  const tickIntervalMs = parseInt(process.env.BOT_TICK_INTERVAL_MS ?? '5000');

  const interval = setInterval(() => runTick(userId, io), tickIntervalMs);
  const priceInterval = setInterval(() => emitPriceUpdates(userId, io), 2000);

  activeBots.set(userId, { userId, interval, priceInterval });
  io.to(`user:${userId}`).emit('bot:status', { running: true, userId });
}

export function stopBot(userId: string, io: Server): void {
  const bot = activeBots.get(userId);
  if (!bot) return;

  clearInterval(bot.interval);
  clearInterval(bot.priceInterval);
  activeBots.delete(userId);

  console.log(`[TradingEngine] Stopped bot for user ${userId}`);
  io.to(`user:${userId}`).emit('bot:status', { running: false, userId });
}

export function isBotRunning(userId: string): boolean {
  return activeBots.has(userId);
}

async function runTick(userId: string, io: Server): Promise<void> {
  try {
    const config = await prisma.botConfig.findUnique({ where: { userId } });
    if (!config || !config.enabled) {
      stopBot(userId, io);
      return;
    }

    const { confidenceThreshold, maxOrderSizeTry, dailyLossLimitTry, watchlist } = config;

    for (const ticker of watchlist) {
      try {
        const candles = await prisma.priceCandle.findMany({
          where: { symbol: { ticker } },
          orderBy: { timestamp: 'desc' },
          take: 200,
        });

        if (candles.length < 30) continue;

        const prices = candles.map(c => c.close).reverse();
        const signal = aggregateSignals(prices);
        const currentPrice = prices[prices.length - 1];

        const tickEvent: BotTickEvent = {
          ticker,
          signal,
          price: currentPrice,
          timestamp: new Date().toISOString(),
        };

        io.to(`user:${userId}`).emit('bot:tick', tickEvent);

        if (signal.confidence >= confidenceThreshold && signal.signal !== 'NEUTRAL') {
          await executeOrder(userId, ticker, signal, maxOrderSizeTry, dailyLossLimitTry);
        }

        // Simulate small price drift on each tick
        await simulatePriceMove(ticker, currentPrice);
      } catch (err) {
        console.error(`[TradingEngine] Error processing ${ticker}:`, err);
      }
    }
  } catch (err) {
    console.error(`[TradingEngine] Tick error for user ${userId}:`, err);
  }
}

async function simulatePriceMove(ticker: string, currentPrice: number): Promise<void> {
  const change = (Math.random() - 0.5) * currentPrice * 0.003;
  const newPrice = Math.max(currentPrice + change, 0.01);

  const symbol = await prisma.bistSymbol.findUnique({ where: { ticker } });
  if (!symbol) return;

  await prisma.bistSymbol.update({
    where: { ticker },
    data: { lastPrice: newPrice },
  });

  // Insert new candle
  await prisma.priceCandle.create({
    data: {
      symbolId: symbol.id,
      open: currentPrice,
      high: Math.max(currentPrice, newPrice) * (1 + Math.random() * 0.002),
      low: Math.min(currentPrice, newPrice) * (1 - Math.random() * 0.002),
      close: newPrice,
      volume: Math.floor(10000 + Math.random() * 50000),
      timestamp: new Date(),
    },
  });
}

async function emitPriceUpdates(userId: string, io: Server): Promise<void> {
  try {
    const config = await prisma.botConfig.findUnique({ where: { userId } });
    if (!config) return;

    const symbols = await prisma.bistSymbol.findMany({
      where: { ticker: { in: config.watchlist } },
      orderBy: { ticker: 'asc' },
    });

    for (const sym of symbols) {
      const candles = await prisma.priceCandle.findMany({
        where: { symbolId: sym.id },
        orderBy: { timestamp: 'desc' },
        take: 2,
      });

      const current = candles[0]?.close ?? sym.lastPrice;
      const prev = candles[1]?.close ?? current;
      const change = current - prev;
      const changePercent = prev !== 0 ? (change / prev) * 100 : 0;

      const event: PriceUpdateEvent = {
        ticker: sym.ticker,
        price: current,
        change,
        changePercent,
        timestamp: new Date().toISOString(),
      };

      io.to(`user:${userId}`).emit('price:update', event);
    }
  } catch (err) {
    console.error(`[TradingEngine] Price emit error:`, err);
  }
}
