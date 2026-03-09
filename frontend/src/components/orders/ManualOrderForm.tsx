import { useState } from 'react';
import { useTradingStore } from '../../stores/tradingStore';
import { useUiStore } from '../../stores/uiStore';

export function ManualOrderForm() {
  const { symbols, placeOrder } = useTradingStore();
  const { addToast } = useUiStore();
  const [ticker, setTicker] = useState('');
  const [type, setType] = useState<'BUY' | 'SELL'>('BUY');
  const [quantity, setQuantity] = useState('');
  const [bank, setBank] = useState('denizbank');
  const [loading, setLoading] = useState(false);

  const selectedSymbol = symbols.find(s => s.ticker === ticker);
  const estimatedTotal = selectedSymbol && quantity ? (selectedSymbol.lastPrice * parseFloat(quantity)).toFixed(2) : null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ticker || !quantity) return;
    setLoading(true);
    try {
      await placeOrder(ticker, type, parseFloat(quantity), bank);
      addToast('success', `${type} order placed for ${ticker}`);
      setQuantity('');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Order failed';
      addToast('error', message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
      <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-4">Place Order</h2>
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="text-xs text-gray-500 mb-1 block">Symbol</label>
            <select
              value={ticker}
              onChange={e => setTicker(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500"
              required
            >
              <option value="">Select symbol...</option>
              {symbols.map(s => (
                <option key={s.ticker} value={s.ticker}>{s.ticker} — {s.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="text-xs text-gray-500 mb-1 block">Order Type</label>
            <div className="flex gap-2">
              {(['BUY', 'SELL'] as const).map(t => (
                <button
                  key={t}
                  type="button"
                  onClick={() => setType(t)}
                  className={`flex-1 py-2 rounded-lg text-sm font-semibold transition-colors ${
                    type === t
                      ? t === 'BUY' ? 'bg-green-600 text-white' : 'bg-red-600 text-white'
                      : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                  }`}
                >
                  {t}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="text-xs text-gray-500 mb-1 block">Quantity</label>
            <input
              type="number"
              min="1"
              step="1"
              value={quantity}
              onChange={e => setQuantity(e.target.value)}
              placeholder="e.g. 10"
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500"
              required
            />
          </div>

          <div>
            <label className="text-xs text-gray-500 mb-1 block">Bank Adapter</label>
            <select
              value={bank}
              onChange={e => setBank(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-200 focus:outline-none focus:border-blue-500"
            >
              <option value="denizbank">DenizBank</option>
              <option value="akbank">Akbank</option>
              <option value="yapikredi">YapıKredi</option>
            </select>
          </div>
        </div>

        {selectedSymbol && (
          <div className="bg-gray-800 rounded-lg px-3 py-2 text-xs text-gray-400 flex justify-between">
            <span>Last Price: <span className="text-gray-200">{selectedSymbol.lastPrice.toFixed(2)} TRY</span></span>
            {estimatedTotal && (
              <span>Est. Total: <span className="text-gray-200">{estimatedTotal} TRY</span></span>
            )}
          </div>
        )}

        <button
          type="submit"
          disabled={loading || !ticker || !quantity}
          className={`w-full py-2.5 rounded-lg text-sm font-semibold transition-colors ${
            type === 'BUY'
              ? 'bg-green-600 hover:bg-green-700 text-white'
              : 'bg-red-600 hover:bg-red-700 text-white'
          } disabled:opacity-40`}
        >
          {loading ? 'Placing...' : `Place ${type} Order`}
        </button>
      </form>
    </div>
  );
}
