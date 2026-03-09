import { BankBalance, BankOrder, BankOrderResult, BankPosition } from '../types';

export interface BankAdapter {
  readonly name: string;
  getBalance(): Promise<BankBalance>;
  placeOrder(order: BankOrder): Promise<BankOrderResult>;
  getPositions(): Promise<BankPosition[]>;
  cancelOrder(orderId: string): Promise<boolean>;
}

export abstract class MockBankAdapterBase implements BankAdapter {
  abstract readonly name: string;

  protected balance = 100_000; // 100k TRY starting balance
  protected positions = new Map<string, { quantity: number; avgPrice: number }>();

  async getBalance(): Promise<BankBalance> {
    const positionValue = Array.from(this.positions.entries()).reduce(
      (sum, [, pos]) => sum + pos.quantity * pos.avgPrice,
      0
    );
    return {
      availableTry: this.balance,
      totalPortfolioValue: this.balance + positionValue,
    };
  }

  async placeOrder(order: BankOrder): Promise<BankOrderResult> {
    // Simulate network latency
    const latency = 200 + Math.random() * 600;
    await new Promise(resolve => setTimeout(resolve, latency));

    // Slight slippage simulation
    const slippage = (Math.random() - 0.5) * 0.002;
    const filledPrice = order.price * (1 + slippage);
    const commission = filledPrice * order.quantity * 0.002; // 0.2% commission

    if (order.type === 'BUY') {
      const totalCost = filledPrice * order.quantity + commission;
      if (this.balance < totalCost) {
        return {
          orderId: `${this.name.toUpperCase()}-${Date.now()}`,
          status: 'FAILED',
          filledPrice: 0,
          filledAt: new Date(),
          commission: 0,
        };
      }
      this.balance -= totalCost;
      const existing = this.positions.get(order.symbolTicker);
      if (existing) {
        const totalQty = existing.quantity + order.quantity;
        const newAvg = (existing.quantity * existing.avgPrice + order.quantity * filledPrice) / totalQty;
        this.positions.set(order.symbolTicker, { quantity: totalQty, avgPrice: newAvg });
      } else {
        this.positions.set(order.symbolTicker, { quantity: order.quantity, avgPrice: filledPrice });
      }
    } else {
      const existing = this.positions.get(order.symbolTicker);
      const availableQty = existing?.quantity ?? 0;
      if (availableQty < order.quantity) {
        return {
          orderId: `${this.name.toUpperCase()}-${Date.now()}`,
          status: 'FAILED',
          filledPrice: 0,
          filledAt: new Date(),
          commission: 0,
        };
      }
      this.balance += filledPrice * order.quantity - commission;
      const remaining = availableQty - order.quantity;
      if (remaining <= 0) {
        this.positions.delete(order.symbolTicker);
      } else {
        this.positions.set(order.symbolTicker, { quantity: remaining, avgPrice: existing!.avgPrice });
      }
    }

    const result: BankOrderResult = {
      orderId: `${this.name.toUpperCase()}-${Date.now()}`,
      status: 'FILLED',
      filledPrice,
      filledAt: new Date(),
      commission,
    };

    console.log(`[${this.name}] Order filled: ${order.type} ${order.quantity} ${order.symbolTicker} @ ${filledPrice.toFixed(2)} TRY`);
    return result;
  }

  async getPositions(): Promise<BankPosition[]> {
    return Array.from(this.positions.entries()).map(([ticker, pos]) => ({
      symbolTicker: ticker,
      quantity: pos.quantity,
      avgPrice: pos.avgPrice,
      currentPrice: pos.avgPrice, // will be updated by caller
      unrealizedPnl: 0,
    }));
  }

  async cancelOrder(_orderId: string): Promise<boolean> {
    await new Promise(resolve => setTimeout(resolve, 100));
    return true;
  }
}
