export type SignalType = 'BUY' | 'SELL' | 'NEUTRAL';
export type OrderType = 'BUY' | 'SELL';
export type OrderStatus = 'PENDING' | 'FILLED' | 'CANCELLED' | 'FAILED';
export type UserRole = 'USER' | 'ADMIN';

export interface User {
  id: string;
  email: string;
  name: string;
  phone?: string;
  role: UserRole;
  createdAt: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface BistSymbol {
  id: string;
  ticker: string;
  name: string;
  sector: string;
  lastPrice: number;
  updatedAt: string;
}

export interface PriceCandle {
  id: string;
  symbolId: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  timestamp: string;
}

export interface IndicatorResult {
  signal: SignalType;
  strength: number;
  value: unknown;
}

export interface AggregatedSignal {
  signal: SignalType;
  strength: number;
  confidence: number;
  components: {
    rsi: IndicatorResult;
    macd: IndicatorResult;
    bollinger: IndicatorResult;
    ema: IndicatorResult;
  };
}

export interface Order {
  id: string;
  userId: string;
  symbolId: string;
  type: OrderType;
  quantity: number;
  price: number;
  status: OrderStatus;
  bankAdapter: string;
  paperTrade: boolean;
  notes?: string;
  createdAt: string;
  filledAt?: string;
  symbol: BistSymbol;
}

export interface Position {
  id: string;
  userId: string;
  symbolId: string;
  quantity: number;
  avgPrice: number;
  unrealizedPnl: number;
  currentPrice?: number;
  createdAt: string;
  updatedAt: string;
  symbol: BistSymbol;
}

export interface Portfolio {
  positions: Position[];
  totalPnl: number;
  totalValue: number;
}

export interface BotConfig {
  id: string;
  userId: string;
  enabled: boolean;
  confidenceThreshold: number;
  maxOrderSizeTry: number;
  dailyLossLimitTry: number;
  tickIntervalMs: number;
  watchlist: string[];
  updatedAt: string;
}

export interface BotStatus {
  running: boolean;
  config: BotConfig | null;
  paperTrading: boolean;
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

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'info' | 'warning';
  message: string;
}
