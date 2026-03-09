import { PrismaClient } from '@prisma/client';
import { createError } from '../middleware/errorHandler';

const prisma = new PrismaClient();

export async function getAllSymbols() {
  return prisma.bistSymbol.findMany({ orderBy: { ticker: 'asc' } });
}

export async function getSymbolByTicker(ticker: string) {
  const symbol = await prisma.bistSymbol.findUnique({ where: { ticker: ticker.toUpperCase() } });
  if (!symbol) throw createError(`Symbol not found: ${ticker}`, 404);
  return symbol;
}

export async function getPriceHistory(ticker: string, limit = 200) {
  const symbol = await prisma.bistSymbol.findUnique({ where: { ticker: ticker.toUpperCase() } });
  if (!symbol) throw createError(`Symbol not found: ${ticker}`, 404);

  const candles = await prisma.priceCandle.findMany({
    where: { symbolId: symbol.id },
    orderBy: { timestamp: 'desc' },
    take: limit,
  });

  return candles.reverse(); // chronological order
}
