import { useTradingStore } from '../../stores/tradingStore';
import { useBotStore } from '../../stores/botStore';
import { SignalBadge } from '../common/Badge';
import { SignalType } from '../../types';

export function SignalTable() {
  const { signals, livePrices, symbols } = useTradingStore();
  const { status } = useBotStore();
  const watchlist = status?.config?.watchlist ?? [];

  const watchedSymbols = symbols.filter(s => watchlist.includes(s.ticker));

  if (watchedSymbols.length === 0) {
    return (
      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-4">Live Signals</h2>
        <p className="text-gray-500 text-sm">No symbols in watchlist</p>
      </div>
    );
  }

  return (
    <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
      <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-4">Live Signals</h2>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-xs text-gray-500 border-b border-gray-800">
              <th className="text-left pb-2 font-medium">Symbol</th>
              <th className="text-right pb-2 font-medium">Price</th>
              <th className="text-right pb-2 font-medium">Change</th>
              <th className="text-center pb-2 font-medium">RSI</th>
              <th className="text-center pb-2 font-medium">MACD</th>
              <th className="text-center pb-2 font-medium">BB</th>
              <th className="text-center pb-2 font-medium">EMA</th>
              <th className="text-center pb-2 font-medium">Overall</th>
              <th className="text-right pb-2 font-medium">Confidence</th>
            </tr>
          </thead>
          <tbody>
            {watchedSymbols.map(sym => {
              const live = livePrices[sym.ticker];
              const sig = signals[sym.ticker];
              const price = live?.price ?? sym.lastPrice;
              const change = live?.changePercent ?? 0;

              return (
                <tr key={sym.ticker} className="border-b border-gray-800/50 hover:bg-gray-800/30">
                  <td className="py-2.5">
                    <div className="font-medium text-gray-200">{sym.ticker}</div>
                    <div className="text-xs text-gray-500">{sym.sector}</div>
                  </td>
                  <td className="text-right font-mono text-gray-200">
                    {price.toFixed(2)}
                  </td>
                  <td className={`text-right font-mono text-xs ${change >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                    {change >= 0 ? '+' : ''}{change.toFixed(2)}%
                  </td>
                  <td className="text-center">
                    {sig ? <SignalBadge signal={sig.components.rsi.signal as SignalType} /> : <span className="text-gray-700">—</span>}
                  </td>
                  <td className="text-center">
                    {sig ? <SignalBadge signal={sig.components.macd.signal as SignalType} /> : <span className="text-gray-700">—</span>}
                  </td>
                  <td className="text-center">
                    {sig ? <SignalBadge signal={sig.components.bollinger.signal as SignalType} /> : <span className="text-gray-700">—</span>}
                  </td>
                  <td className="text-center">
                    {sig ? <SignalBadge signal={sig.components.ema.signal as SignalType} /> : <span className="text-gray-700">—</span>}
                  </td>
                  <td className="text-center">
                    {sig ? <SignalBadge signal={sig.signal} /> : <span className="text-gray-700">—</span>}
                  </td>
                  <td className="text-right font-mono text-xs text-gray-400">
                    {sig ? `${(sig.confidence * 100).toFixed(1)}%` : '—'}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
