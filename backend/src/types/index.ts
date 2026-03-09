export type SignalType = 'BUY' | 'SELL' | 'NEUTRAL';
export type OrderType = 'BUY' | 'SELL';
export type OrderStatus = 'PENDING' | 'FILLED' | 'CANCELLED' | 'FAILED';
export type BankName = 'denizbank' | 'akbank' | 'yapikredi' | 'mock';

export interface IndicatorResult {
  signal: SignalType;
  strength: number; // 0 to 1
  value: unknown;
}

export interface AggregatedSignal {
  signal: SignalType;
  strength: number;
  confidence: number; // 0 to 1
  components: {
    rsi: IndicatorResult;
    macd: IndicatorResult;
    bollinger: IndicatorResult;
    ema: IndicatorResult;
  };
}

export interface PriceData {
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  timestamp: Date;
}

export interface BankOrder {
  symbolTicker: string;
  type: OrderType;
  quantity: number;
  price: number;
  paperTrade: boolean;
}

export interface BankOrderResult {
  orderId: string;
  status: OrderStatus;
  filledPrice: number;
  filledAt: Date;
  commission: number;
}

export interface BankPosition {
  symbolTicker: string;
  quantity: number;
  avgPrice: number;
  currentPrice: number;
  unrealizedPnl: number;
}

export interface BankBalance {
  availableTry: number;
  totalPortfolioValue: number;
}

export interface JwtPayload {
  userId: string;
  email: string;
  role: string;
}

export interface BotTickEvent {
  ticker: string;
  signal: AggregatedSignal;
  price: number;
  timestamp: string;
}

export interface PriceUpdateEvent {
  ticker: string;
  price: number;
  change: number;
  changePercent: number;
  timestamp: string;
}

// Augment Express Request
declare global {
  namespace Express {
    interface Request {
      user?: JwtPayload;
    }
  }
}
