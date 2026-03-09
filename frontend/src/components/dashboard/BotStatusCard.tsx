import { useBotStore } from '../../stores/botStore';
import { useUiStore } from '../../stores/uiStore';

export function BotStatusCard() {
  const { status, startBot, stopBot, isLoading } = useBotStore();
  const { addToast } = useUiStore();

  const handleToggle = async () => {
    try {
      if (status?.running) {
        await stopBot();
        addToast('info', 'Bot stopped');
      } else {
        await startBot();
        addToast('success', 'Bot started');
      }
    } catch {
      addToast('error', 'Failed to toggle bot');
    }
  };

  if (!status) return null;

  const { running, config, paperTrading } = status;

  return (
    <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide">Bot Status</h2>
        <button
          onClick={handleToggle}
          disabled={isLoading}
          className={`px-4 py-2 rounded-lg text-sm font-semibold transition-colors ${
            running
              ? 'bg-red-600 hover:bg-red-700 text-white'
              : 'bg-green-600 hover:bg-green-700 text-white'
          } disabled:opacity-50`}
        >
          {running ? 'Stop Bot' : 'Start Bot'}
        </button>
      </div>

      <div className="grid grid-cols-2 gap-3 text-sm">
        <div className="bg-gray-800 rounded-lg p-3">
          <div className="text-gray-500 text-xs mb-1">Status</div>
          <div className={`font-semibold ${running ? 'text-green-400' : 'text-gray-400'}`}>
            {running ? '● Running' : '○ Stopped'}
          </div>
        </div>
        <div className="bg-gray-800 rounded-lg p-3">
          <div className="text-gray-500 text-xs mb-1">Mode</div>
          <div className={`font-semibold ${paperTrading ? 'text-yellow-400' : 'text-blue-400'}`}>
            {paperTrading ? 'Paper' : 'Live'}
          </div>
        </div>
        <div className="bg-gray-800 rounded-lg p-3">
          <div className="text-gray-500 text-xs mb-1">Confidence Threshold</div>
          <div className="font-semibold text-gray-200">
            {((config?.confidenceThreshold ?? 0.65) * 100).toFixed(0)}%
          </div>
        </div>
        <div className="bg-gray-800 rounded-lg p-3">
          <div className="text-gray-500 text-xs mb-1">Max Order</div>
          <div className="font-semibold text-gray-200">
            {(config?.maxOrderSizeTry ?? 10000).toLocaleString()} TRY
          </div>
        </div>
      </div>

      {config && config.watchlist.length > 0 && (
        <div className="mt-3">
          <div className="text-gray-500 text-xs mb-1">Watchlist</div>
          <div className="flex flex-wrap gap-1">
            {config.watchlist.map(ticker => (
              <span key={ticker} className="text-xs bg-blue-500/10 text-blue-400 border border-blue-500/20 px-2 py-0.5 rounded">
                {ticker}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
