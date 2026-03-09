import { PrismaClient, OrderType, OrderStatus } from '@prisma/client';
import { createError } from '../middleware/errorHandler';
import { getBankAdapter } from '../banks/factory';
import { BankOrder } from '../types';

const prisma = new PrismaClient();

export async function getOrders(userId: string, limit = 50) {
  return prisma.order.findMany({
    where: { userId },
    include: { symbol: true },
    orderBy: { createdAt: 'desc' },
    take: limit,
  });
}

export async function placeManualOrder(
  userId: string,
  ticker: string,
  type: OrderType,
  quantity: number,
  bankAdapterName = 'denizbank'
) {
  const symbol = await prisma.bistSymbol.findUnique({ where: { ticker: ticker.toUpperCase() } });
  if (!symbol) throw createError(`Symbol not found: ${ticker}`, 404);

  if (quantity <= 0) throw createError('Quantity must be positive', 400);

  const price = symbol.lastPrice;
  const paperTrade = process.env.PAPER_TRADING_MODE !== 'false';

  const order = await prisma.order.create({
    data: {
      userId,
      symbolId: symbol.id,
      type,
      quantity,
      price,
      status: 'PENDING',
      bankAdapter: bankAdapterName,
      paperTrade,
      notes: 'Manual order',
    },
    include: { symbol: true },
  });

  const adapter = getBankAdapter(bankAdapterName as 'denizbank' | 'akbank' | 'yapikredi');
  const bankOrder: BankOrder = { symbolTicker: ticker, type, quantity, price, paperTrade };

  try {
    const result = await adapter.placeOrder(bankOrder);
    const filled = await prisma.order.update({
      where: { id: order.id },
      data: { status: result.status, price: result.filledPrice, filledAt: result.filledAt },
      include: { symbol: true },
    });

    if (result.status === 'FILLED') {
      await upsertPosition(userId, symbol.id, type, quantity, result.filledPrice);
    }

    return filled;
  } catch (err) {
    console.error(`[TradingService] Order execution failed:`, err);
    await prisma.order.update({ where: { id: order.id }, data: { status: 'FAILED' } });
    throw createError('Order execution failed', 500);
  }
}

async function upsertPosition(
  userId: string,
  symbolId: string,
  type: OrderType,
  qty: number,
  price: number
) {
  if (type === 'BUY') {
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
  } else {
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
}

export async function cancelOrder(userId: string, orderId: string) {
  const order = await prisma.order.findUnique({ where: { id: orderId } });
  if (!order) throw createError('Order not found', 404);
  if (order.userId !== userId) throw createError('Not authorized', 403);
  if (order.status !== 'PENDING') throw createError('Can only cancel pending orders', 400);

  return prisma.order.update({
    where: { id: orderId },
    data: { status: 'CANCELLED' },
    include: { symbol: true },
  });
}

export async function getPortfolio(userId: string) {
  const positions = await prisma.position.findMany({
    where: { userId },
    include: { symbol: true },
  });

  // Update unrealized P&L with current prices
  const updatedPositions = await Promise.all(
    positions.map(async pos => {
      const currentPrice = pos.symbol.lastPrice;
      const unrealizedPnl = (currentPrice - pos.avgPrice) * pos.quantity;
      await prisma.position.update({
        where: { id: pos.id },
        data: { unrealizedPnl },
      });
      return { ...pos, unrealizedPnl, currentPrice };
    })
  );

  const totalPnl = updatedPositions.reduce((sum, p) => sum + p.unrealizedPnl, 0);
  const totalValue = updatedPositions.reduce(
    (sum, p) => sum + p.symbol.lastPrice * p.quantity,
    0
  );

  return { positions: updatedPositions, totalPnl, totalValue };
}
