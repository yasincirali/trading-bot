import client from './client';
import { BotStatus, BotConfig } from '../types';

export const botApi = {
  async getStatus() {
    const { data } = await client.get<BotStatus>('/bot/status');
    return data;
  },

  async start() {
    const { data } = await client.post<{ running: boolean; message: string }>('/bot/start');
    return data;
  },

  async stop() {
    const { data } = await client.post<{ running: boolean; message: string }>('/bot/stop');
    return data;
  },

  async getConfig() {
    const { data } = await client.get<BotConfig>('/bot/config');
    return data;
  },

  async updateConfig(config: Partial<BotConfig>) {
    const { data } = await client.put<BotConfig>('/bot/config', config);
    return data;
  },

  async addToWatchlist(ticker: string) {
    const { data } = await client.post<BotConfig>(`/bot/watchlist/${ticker}`);
    return data;
  },

  async removeFromWatchlist(ticker: string) {
    const { data } = await client.delete<BotConfig>(`/bot/watchlist/${ticker}`);
    return data;
  },
};
