import { useTradingStore } from '../../stores/tradingStore';

export function PnlSummary() {
  const { portfolio } = useTradingStore();

  if (!portfolio) return null;

  const { totalPnl, totalValue, positions } = portfolio;

  return (
    <div className="grid grid-cols-3 gap-4">
      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <div className="text-xs text-gray-500 uppercase tracking-wide mb-1">Portfolio Value</div>
        <div className="text-2xl font-bold text-gray-100">{totalValue.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} TRY</div>
      </div>

      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <div className="text-xs text-gray-500 uppercase tracking-wide mb-1">Unrealized P&L</div>
        <div className={`text-2xl font-bold ${totalPnl >= 0 ? 'text-green-400' : 'text-red-400'}`}>
          {totalPnl >= 0 ? '+' : ''}{totalPnl.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} TRY
        </div>
      </div>

      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <div className="text-xs text-gray-500 uppercase tracking-wide mb-1">Open Positions</div>
        <div className="text-2xl font-bold text-gray-100">{positions.length}</div>
      </div>
    </div>
  );
}
