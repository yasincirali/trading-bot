import { create } from 'zustand';
import { BistSymbol, Order, Portfolio, PriceUpdateEvent, AggregatedSignal } from '../types';
import { symbolsApi, ordersApi, portfolioApi } from '../api/trading';

interface LivePrice {
  price: number;
  change: number;
  changePercent: number;
}

interface TradingState {
  symbols: BistSymbol[];
  orders: Order[];
  portfolio: Portfolio | null;
  livePrices: Record<string, LivePrice>;
  signals: Record<string, AggregatedSignal>;
  isLoading: boolean;

  fetchSymbols: () => Promise<void>;
  fetchOrders: () => Promise<void>;
  fetchPortfolio: () => Promise<void>;
  updateLivePrice: (event: PriceUpdateEvent) => void;
  updateSignal: (ticker: string, signal: AggregatedSignal) => void;
  placeOrder: (ticker: string, type: 'BUY' | 'SELL', quantity: number, bankAdapter?: string) => Promise<Order>;
  cancelOrder: (id: string) => Promise<void>;
  addSymbol: (ticker: string, type?: 'STOCK' | 'FUND' | 'FOREX' | 'COMMODITY') => Promise<BistSymbol>;
  removeSymbol: (ticker: string) => Promise<void>;
}

export const useTradingStore = create<TradingState>((set, get) => ({
  symbols: [],
  orders: [],
  portfolio: null,
  livePrices: {},
  signals: {},
  isLoading: false,

  fetchSymbols: async () => {
    const symbols = await symbolsApi.getAll();
    set({ symbols });
  },

  fetchOrders: async () => {
    const orders = await ordersApi.getAll();
    set({ orders });
  },

  fetchPortfolio: async () => {
    const portfolio = await portfolioApi.get();
    set({ portfolio });
  },

  updateLivePrice: (event) => {
    set(state => ({
      livePrices: {
        ...state.livePrices,
        [event.ticker]: {
          price: event.price,
          change: event.change,
          changePercent: event.changePercent,
        },
      },
      symbols: state.symbols.map(s =>
        s.ticker === event.ticker ? { ...s, lastPrice: event.price } : s
      ),
    }));
  },

  updateSignal: (ticker, signal) => {
    set(state => ({
      signals: { ...state.signals, [ticker]: signal },
    }));
  },

  placeOrder: async (ticker, type, quantity, bankAdapter = 'denizbank') => {
    const order = await ordersApi.place(ticker, type, quantity, bankAdapter);
    set(state => ({ orders: [order, ...state.orders] }));
    return order;
  },

  cancelOrder: async (id) => {
    await ordersApi.cancel(id);
    set(state => ({
      orders: state.orders.map(o => o.id === id ? { ...o, status: 'CANCELLED' } : o),
    }));
  },

  addSymbol: async (ticker, type) => {
    const symbol = await symbolsApi.add(ticker, type);
    set(state => ({
      symbols: state.symbols.some(s => s.ticker === symbol.ticker)
        ? state.symbols
        : [...state.symbols, symbol],
    }));
    return symbol;
  },

  removeSymbol: async (ticker) => {
    await symbolsApi.remove(ticker);
    set(state => ({
      symbols: state.symbols.filter(s => s.ticker !== ticker),
    }));
  },
}));
