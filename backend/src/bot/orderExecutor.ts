import { PrismaClient } from '@prisma/client';
import { AggregatedSignal, BankOrder, OrderType } from '../types';
import { getDefaultAdapter } from '../banks/factory';

const prisma = new PrismaClient();

function isWithinTradingHours(): boolean {
  const now = new Date();
  // Turkey is UTC+3
  const utcMs = now.getTime() + now.getTimezoneOffset() * 60_000;
  const turkeyTime = new Date(utcMs + 3 * 3_600_000);
  const hour = turkeyTime.getHours();
  const minute = turkeyTime.getMinutes();
  const totalMinutes = hour * 60 + minute;
  // 10:00 to 18:00 Turkey time
  return totalMinutes >= 600 && totalMinutes < 1080;
}

export async function executeOrder(
  userId: string,
  ticker: string,
  signal: AggregatedSignal,
  maxOrderSizeTry: number,
  dailyLossLimitTry: number
): Promise<void> {
  const paperTrade = process.env.PAPER_TRADING_MODE !== 'false';

  if (!isWithinTradingHours()) {
    console.log(`[OrderExecutor] Outside BIST trading hours, skipping ${ticker}`);
    return;
  }

  // Check daily loss
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const todayOrders = await prisma.order.findMany({
    where: {
      userId,
      createdAt: { gte: today },
      status: 'FILLED',
      type: 'SELL',
    },
  });

  // Simplified daily loss check (based on filled sell orders)
  const dailyLoss = todayOrders.reduce((sum, o) => sum + o.price * o.quantity * 0.002, 0);
  if (dailyLoss >= dailyLossLimitTry) {
    console.log(`[OrderExecutor] Daily loss limit reached for user ${userId}`);
    return;
  }

  const symbol = await prisma.bistSymbol.findUnique({ where: { ticker } });
  if (!symbol) {
    console.error(`[OrderExecutor] Symbol not found: ${ticker}`);
    return;
  }

  const price = symbol.lastPrice;
  if (price <= 0) return;

  const orderType: OrderType = signal.signal === 'BUY' ? 'BUY' : 'SELL';
  const quantity = Math.floor(maxOrderSizeTry / price);
  if (quantity <= 0) return;

  // For SELL, check if user has position
  if (orderType === 'SELL') {
    const position = await prisma.position.findUnique({
      where: { userId_symbolId: { userId, symbolId: symbol.id } },
    });
    if (!position || position.quantity <= 0) {
      console.log(`[OrderExecutor] No position to sell for ${ticker}`);
      return;
    }
  }

  const adapter = getDefaultAdapter();
  const bankOrder: BankOrder = {
    symbolTicker: ticker,
    type: orderType,
    quantity,
    price,
    paperTrade,
  };

  const order = await prisma.order.create({
    data: {
      userId,
      symbolId: symbol.id,
      type: orderType,
      quantity,
      price,
      status: 'PENDING',
      bankAdapter: adapter.name,
      paperTrade,
      notes: `Auto: confidence=${signal.confidence.toFixed(3)}`,
    },
  });

  try {
    const result = await adapter.placeOrder(bankOrder);

    await prisma.order.update({
      where: { id: order.id },
      data: {
        status: result.status,
        price: result.filledPrice,
        filledAt: result.filledAt,
      },
    });

    if (result.status === 'FILLED') {
      if (orderType === 'BUY') {
        await upsertPosition(userId, symbol.id, quantity, result.filledPrice);
      } else {
        await reducePosition(userId, symbol.id, quantity);
      }
    }

    console.log(`[OrderExecutor] ${orderType} ${quantity} ${ticker} @ ${result.filledPrice.toFixed(2)} — ${result.status}`);
  } catch (err) {
    console.error(`[OrderExecutor] Failed to execute order for ${ticker}:`, err);
    await prisma.order.update({ where: { id: order.id }, data: { status: 'FAILED' } });
  }
}

async function upsertPosition(userId: string, symbolId: string, qty: number, price: number): Promise<void> {
  const existing = await prisma.position.findUnique({
    where: { userId_symbolId: { userId, symbolId } },
  });

  if (existing) {
    const totalQty = existing.quantity + qty;
    const newAvg = (existing.quantity * existing.avgPrice + qty * price) / totalQty;
    await prisma.position.update({
      where: { userId_symbolId: { userId, symbolId } },
      data: { quantity: totalQty, avgPrice: newAvg },
    });
  } else {
    await prisma.position.create({ data: { userId, symbolId, quantity: qty, avgPrice: price } });
  }
}

async function reducePosition(userId: string, symbolId: string, qty: number): Promise<void> {
  const existing = await prisma.position.findUnique({
    where: { userId_symbolId: { userId, symbolId } },
  });

  if (!existing) return;
  const remaining = existing.quantity - qty;
  if (remaining <= 0) {
    await prisma.position.delete({ where: { userId_symbolId: { userId, symbolId } } });
  } else {
    await prisma.position.update({
      where: { userId_symbolId: { userId, symbolId } },
      data: { quantity: remaining },
    });
  }
}
