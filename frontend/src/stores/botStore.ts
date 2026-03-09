import { create } from 'zustand';
import { BotConfig, BotStatus, BotTickEvent } from '../types';
import { botApi } from '../api/bot';

interface BotState {
  status: BotStatus | null;
  recentTicks: BotTickEvent[];
  isLoading: boolean;

  fetchStatus: () => Promise<void>;
  startBot: () => Promise<void>;
  stopBot: () => Promise<void>;
  updateConfig: (config: Partial<BotConfig>) => Promise<void>;
  addTick: (tick: BotTickEvent) => void;
  setRunning: (running: boolean) => void;
}

export const useBotStore = create<BotState>((set, get) => ({
  status: null,
  recentTicks: [],
  isLoading: false,

  fetchStatus: async () => {
    set({ isLoading: true });
    try {
      const status = await botApi.getStatus();
      set({ status, isLoading: false });
    } catch {
      set({ isLoading: false });
    }
  },

  startBot: async () => {
    await botApi.start();
    set(state => ({
      status: state.status ? { ...state.status, running: true } : null,
    }));
  },

  stopBot: async () => {
    await botApi.stop();
    set(state => ({
      status: state.status ? { ...state.status, running: false } : null,
    }));
  },

  updateConfig: async (config) => {
    const updated = await botApi.updateConfig(config);
    set(state => ({
      status: state.status ? { ...state.status, config: updated } : null,
    }));
  },

  addTick: (tick) => {
    set(state => ({
      recentTicks: [tick, ...state.recentTicks].slice(0, 100),
    }));
  },

  setRunning: (running) => {
    set(state => ({
      status: state.status ? { ...state.status, running } : null,
    }));
  },
}));
