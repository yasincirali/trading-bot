import { useTradingStore } from '../../stores/tradingStore';

export function PositionTable() {
  const { portfolio, livePrices } = useTradingStore();
  const positions = portfolio?.positions ?? [];

  if (positions.length === 0) {
    return <p className="text-gray-500 text-sm py-8 text-center">No open positions</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-xs text-gray-500 border-b border-gray-800">
            <th className="text-left pb-3 font-medium">Symbol</th>
            <th className="text-right pb-3 font-medium">Quantity</th>
            <th className="text-right pb-3 font-medium">Avg Price</th>
            <th className="text-right pb-3 font-medium">Current Price</th>
            <th className="text-right pb-3 font-medium">Market Value</th>
            <th className="text-right pb-3 font-medium">P&L</th>
            <th className="text-right pb-3 font-medium">P&L %</th>
          </tr>
        </thead>
        <tbody>
          {positions.map(pos => {
            const livePrice = livePrices[pos.symbol.ticker]?.price ?? pos.symbol.lastPrice;
            const marketValue = livePrice * pos.quantity;
            const costBasis = pos.avgPrice * pos.quantity;
            const pnl = marketValue - costBasis;
            const pnlPct = costBasis !== 0 ? (pnl / costBasis) * 100 : 0;

            return (
              <tr key={pos.id} className="border-b border-gray-800/40 hover:bg-gray-800/20">
                <td className="py-3">
                  <div className="font-medium text-gray-200">{pos.symbol.ticker}</div>
                  <div className="text-xs text-gray-500">{pos.symbol.sector}</div>
                </td>
                <td className="text-right font-mono text-gray-300">{pos.quantity.toLocaleString()}</td>
                <td className="text-right font-mono text-gray-400">{pos.avgPrice.toFixed(2)}</td>
                <td className="text-right font-mono text-gray-200">{livePrice.toFixed(2)}</td>
                <td className="text-right font-mono text-gray-300">{marketValue.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</td>
                <td className={`text-right font-mono font-medium ${pnl >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                  {pnl >= 0 ? '+' : ''}{pnl.toFixed(2)}
                </td>
                <td className={`text-right font-mono text-xs ${pnlPct >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                  {pnlPct >= 0 ? '+' : ''}{pnlPct.toFixed(2)}%
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
