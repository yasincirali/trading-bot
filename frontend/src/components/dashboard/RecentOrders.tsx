import { useTradingStore } from '../../stores/tradingStore';
import { OrderTypeBadge, OrderStatusBadge } from '../common/Badge';

export function RecentOrders() {
  const { orders } = useTradingStore();
  const recent = orders.slice(0, 5);

  return (
    <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
      <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-4">Recent Orders</h2>
      {recent.length === 0 ? (
        <p className="text-gray-500 text-sm">No orders yet</p>
      ) : (
        <div className="space-y-2">
          {recent.map(order => (
            <div key={order.id} className="flex items-center justify-between py-2 border-b border-gray-800/50 last:border-0">
              <div className="flex items-center gap-2">
                <OrderTypeBadge type={order.type} />
                <span className="font-medium text-gray-200 text-sm">{order.symbol.ticker}</span>
                <span className="text-gray-500 text-xs">{order.quantity} @ {order.price.toFixed(2)}</span>
              </div>
              <div className="flex items-center gap-2">
                <OrderStatusBadge status={order.status} />
                {order.paperTrade && (
                  <span className="text-xs text-yellow-600">paper</span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
