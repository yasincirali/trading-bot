import { useState, useEffect } from 'react';
import { useBotStore } from '../../stores/botStore';
import { useTradingStore } from '../../stores/tradingStore';
import { useUiStore } from '../../stores/uiStore';

export function BotConfigForm() {
  const { status, updateConfig, addToWatchlist, removeFromWatchlist } = useBotStore();
  const { symbols, fetchSymbols, addSymbol } = useTradingStore();
  const { addToast } = useUiStore();
  const config = status?.config;

  const [threshold, setThreshold] = useState('0.65');
  const [maxOrder, setMaxOrder] = useState('10000');
  const [dailyLimit, setDailyLimit] = useState('5000');
  const [tickInterval, setTickInterval] = useState('5000');
  const [loading, setLoading] = useState(false);

  const [newTicker, setNewTicker] = useState('');
  const [newType, setNewType] = useState<'STOCK' | 'FUND'>('STOCK');
  const [addingTicker, setAddingTicker] = useState<string | null>(null);
  const [removingTicker, setRemovingTicker] = useState<string | null>(null);

  useEffect(() => {
    fetchSymbols();
  }, []);

  useEffect(() => {
    if (config) {
      setThreshold(config.confidenceThreshold.toString());
      setMaxOrder(config.maxOrderSizeTry.toString());
      setDailyLimit(config.dailyLossLimitTry.toString());
      setTickInterval(config.tickIntervalMs.toString());
    }
  }, [config]);

  const watchlist = config?.watchlist ?? [];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await updateConfig({
        confidenceThreshold: parseFloat(threshold),
        maxOrderSizeTry: parseFloat(maxOrder),
        dailyLossLimitTry: parseFloat(dailyLimit),
        tickIntervalMs: parseInt(tickInterval),
      });
      addToast('success', 'Bot configuration saved');
    } catch {
      addToast('error', 'Failed to save configuration');
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = async (ticker: string, type?: 'STOCK' | 'FUND') => {
    const t = ticker.toUpperCase().trim();
    if (!t) return;
    setAddingTicker(t);
    try {
      // Ensure symbol exists in system (fetches from Yahoo Finance if new)
      if (!symbols.some(s => s.ticker === t)) {
        await addSymbol(t, type);
      }
      await addToWatchlist(t);
      addToast('success', `${t} izleme listesine eklendi`);
      setNewTicker('');
    } catch {
      addToast('error', `${t} eklenemedi`);
    } finally {
      setAddingTicker(null);
    }
  };

  const handleRemove = async (ticker: string) => {
    setRemovingTicker(ticker);
    try {
      await removeFromWatchlist(ticker);
      addToast('success', `${ticker} izleme listesinden çıkarıldı`);
    } catch {
      addToast('error', `${ticker} çıkarılamadı`);
    } finally {
      setRemovingTicker(null);
    }
  };

  const availableSymbols = symbols.filter(s => !watchlist.includes(s.ticker));

  return (
    <div className="bg-gray-900 rounded-xl border border-gray-800 p-5 space-y-6">
      <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide">Bot Configuration</h2>

      {/* Numeric settings */}
      <form onSubmit={handleSubmit} className="space-y-4 max-w-lg">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-xs text-gray-500 mb-1 block">Confidence Threshold (0–1)</label>
            <input
              type="number" min="0" max="1" step="0.01"
              value={threshold}
              onChange={e => setThreshold(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 mb-1 block">Max Order Size (TRY)</label>
            <input
              type="number" min="100" step="100"
              value={maxOrder}
              onChange={e => setMaxOrder(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 mb-1 block">Daily Loss Limit (TRY)</label>
            <input
              type="number" min="100" step="100"
              value={dailyLimit}
              onChange={e => setDailyLimit(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 mb-1 block">Tick Interval (ms)</label>
            <input
              type="number" min="1000" step="1000"
              value={tickInterval}
              onChange={e => setTickInterval(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500"
            />
          </div>
        </div>
        <button
          type="submit"
          disabled={loading}
          className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition-colors disabled:opacity-40"
        >
          {loading ? 'Kaydediliyor...' : 'Ayarları Kaydet'}
        </button>
      </form>

      {/* Watchlist section */}
      <div className="space-y-3">
        <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wide">İzleme Listesi</h3>

        {/* Current watchlist chips */}
        <div className="flex flex-wrap gap-2 min-h-8">
          {watchlist.length === 0 && (
            <span className="text-xs text-gray-600">Henüz hisse eklenmedi</span>
          )}
          {watchlist.map(ticker => {
            const sym = symbols.find(s => s.ticker === ticker);
            return (
              <span
                key={ticker}
                className="inline-flex items-center gap-1.5 pl-2.5 pr-1.5 py-1 bg-blue-900/50 border border-blue-700/50 rounded-full text-xs text-blue-200"
              >
                {ticker}
                {sym && (
                  <span className="text-[10px] text-blue-400/70 font-medium">
                    {sym.type === 'FUND' ? 'FON' : 'BIST'}
                  </span>
                )}
                <button
                  type="button"
                  disabled={removingTicker === ticker}
                  onClick={() => handleRemove(ticker)}
                  className="ml-0.5 w-4 h-4 flex items-center justify-center rounded-full hover:bg-red-500/30 text-blue-400 hover:text-red-300 transition-colors disabled:opacity-40"
                >
                  ×
                </button>
              </span>
            );
          })}
        </div>

        {/* Add new ticker input */}
        <div className="flex gap-2 items-center">
          <input
            type="text"
            placeholder="Sembol (örn. THYAO)"
            value={newTicker}
            onChange={e => setNewTicker(e.target.value.toUpperCase())}
            onKeyDown={e => e.key === 'Enter' && (e.preventDefault(), handleAdd(newTicker, newType))}
            className="flex-1 max-w-[180px] bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500 placeholder:text-gray-600"
          />
          <select
            value={newType}
            onChange={e => setNewType(e.target.value as 'STOCK' | 'FUND')}
            className="bg-gray-800 border border-gray-700 rounded-lg px-2 py-2 text-sm text-gray-300 focus:outline-none focus:border-blue-500"
          >
            <option value="STOCK">Hisse</option>
            <option value="FUND">Fon</option>
          </select>
          <button
            type="button"
            disabled={!newTicker.trim() || !!addingTicker}
            onClick={() => handleAdd(newTicker, newType)}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition-colors disabled:opacity-40"
          >
            {addingTicker ? '...' : 'Ekle'}
          </button>
        </div>

        {/* Quick-add from available system symbols */}
        {availableSymbols.length > 0 && (
          <div>
            <p className="text-xs text-gray-600 mb-1.5">Sistemdeki semboller:</p>
            <div className="flex flex-wrap gap-1.5">
              {availableSymbols.map(sym => (
                <button
                  key={sym.ticker}
                  type="button"
                  disabled={!!addingTicker}
                  onClick={() => handleAdd(sym.ticker, sym.type)}
                  className="inline-flex items-center gap-1 px-2.5 py-1 bg-gray-800 hover:bg-gray-700 border border-gray-700 rounded-lg text-xs text-gray-300 transition-colors disabled:opacity-40"
                >
                  {sym.ticker}
                  <span className="text-[10px] text-gray-500">{sym.type === 'FUND' ? 'FON' : 'BIST'}</span>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
