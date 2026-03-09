import { useState } from 'react';
import { BotStatusCard } from '../components/dashboard/BotStatusCard';
import { SignalTable } from '../components/dashboard/SignalTable';
import { RecentOrders } from '../components/dashboard/RecentOrders';
import { PriceChart } from '../components/chart/PriceChart';
import { useBotStore } from '../stores/botStore';

export function DashboardPage() {
  const { status } = useBotStore();
  const watchlist = status?.config?.watchlist ?? [];
  const [selectedTicker, setSelectedTicker] = useState(watchlist[0] ?? 'THYAO');

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold text-gray-100">Dashboard</h1>
        <p className="text-gray-500 text-sm mt-0.5">Real-time signals & bot control</p>
      </div>

      <div className="grid grid-cols-3 gap-5">
        <div className="col-span-1">
          <BotStatusCard />
        </div>
        <div className="col-span-2">
          <RecentOrders />
        </div>
      </div>

      <SignalTable />

      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide">Price Chart</h2>
          <div className="flex gap-1">
            {(watchlist.length > 0 ? watchlist : ['THYAO', 'GARAN', 'AKBNK']).map(ticker => (
              <button
                key={ticker}
                onClick={() => setSelectedTicker(ticker)}
                className={`px-3 py-1 rounded-lg text-xs font-medium transition-colors ${
                  selectedTicker === ticker ? 'bg-blue-600 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                }`}
              >
                {ticker}
              </button>
            ))}
          </div>
        </div>
        <PriceChart ticker={selectedTicker} />
      </div>
    </div>
  );
}
