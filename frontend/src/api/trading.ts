import client from './client';
import { BistSymbol, PriceCandle, AggregatedSignal, Order, Portfolio } from '../types';

export const symbolsApi = {
  async getAll() {
    const { data } = await client.get<BistSymbol[]>('/symbols');
    return data;
  },

  async get(ticker: string) {
    const { data } = await client.get<BistSymbol>(`/symbols/${ticker}`);
    return data;
  },

  async getCandles(ticker: string, limit = 200) {
    const { data } = await client.get<PriceCandle[]>(`/symbols/${ticker}/candles`, {
      params: { limit },
    });
    return data;
  },

  async getSignals(ticker: string) {
    const { data } = await client.get<AggregatedSignal>(`/symbols/${ticker}/signals`);
    return data;
  },

  async add(ticker: string, type?: 'STOCK' | 'FUND' | 'FOREX' | 'COMMODITY') {
    const { data } = await client.post<BistSymbol>('/symbols', { ticker, type });
    return data;
  },

  async remove(ticker: string) {
    await client.delete(`/symbols/${ticker}`);
  },
};

export const ordersApi = {
  async getAll(limit = 50) {
    const { data } = await client.get<Order[]>('/orders', { params: { limit } });
    return data;
  },

  async place(ticker: string, type: 'BUY' | 'SELL', quantity: number, bankAdapter = 'denizbank') {
    const { data } = await client.post<Order>('/orders', { ticker, type, quantity, bankAdapter });
    return data;
  },

  async cancel(id: string) {
    const { data } = await client.delete<Order>(`/orders/${id}`);
    return data;
  },
};

export const portfolioApi = {
  async get() {
    const { data } = await client.get<Portfolio>('/portfolio');
    return data;
  },
};
