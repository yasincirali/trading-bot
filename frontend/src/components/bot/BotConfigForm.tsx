import { useState, useEffect } from 'react';
import { useBotStore } from '../../stores/botStore';
import { useUiStore } from '../../stores/uiStore';

const ALL_TICKERS = ['THYAO', 'EREGL', 'GARAN', 'ASELS', 'SISE', 'KCHOL', 'BIMAS', 'SAHOL', 'TUPRS', 'AKBNK'];

export function BotConfigForm() {
  const { status, updateConfig } = useBotStore();
  const { addToast } = useUiStore();
  const config = status?.config;

  const [threshold, setThreshold] = useState('0.65');
  const [maxOrder, setMaxOrder] = useState('10000');
  const [dailyLimit, setDailyLimit] = useState('5000');
  const [tickInterval, setTickInterval] = useState('5000');
  const [watchlist, setWatchlist] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (config) {
      setThreshold(config.confidenceThreshold.toString());
      setMaxOrder(config.maxOrderSizeTry.toString());
      setDailyLimit(config.dailyLossLimitTry.toString());
      setTickInterval(config.tickIntervalMs.toString());
      setWatchlist(config.watchlist);
    }
  }, [config]);

  const toggleTicker = (ticker: string) => {
    setWatchlist(prev =>
      prev.includes(ticker) ? prev.filter(t => t !== ticker) : [...prev, ticker]
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await updateConfig({
        confidenceThreshold: parseFloat(threshold),
        maxOrderSizeTry: parseFloat(maxOrder),
        dailyLossLimitTry: parseFloat(dailyLimit),
        tickIntervalMs: parseInt(tickInterval),
        watchlist,
      });
      addToast('success', 'Bot configuration saved');
    } catch {
      addToast('error', 'Failed to save configuration');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
      <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-5">Bot Configuration</h2>
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

        <div>
          <label className="text-xs text-gray-500 mb-2 block">Watchlist</label>
          <div className="flex flex-wrap gap-2">
            {ALL_TICKERS.map(ticker => (
              <button
                key={ticker}
                type="button"
                onClick={() => toggleTicker(ticker)}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                  watchlist.includes(ticker)
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                }`}
              >
                {ticker}
              </button>
            ))}
          </div>
        </div>

        <button
          type="submit"
          disabled={loading}
          className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition-colors disabled:opacity-40"
        >
          {loading ? 'Saving...' : 'Save Configuration'}
        </button>
      </form>
    </div>
  );
}
