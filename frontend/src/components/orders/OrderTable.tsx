import { useTradingStore } from '../../stores/tradingStore';
import { useUiStore } from '../../stores/uiStore';
import { OrderTypeBadge, OrderStatusBadge } from '../common/Badge';

export function OrderTable() {
  const { orders, cancelOrder } = useTradingStore();
  const { addToast } = useUiStore();

  const handleCancel = async (id: string) => {
    try {
      await cancelOrder(id);
      addToast('success', 'Order cancelled');
    } catch {
      addToast('error', 'Failed to cancel order');
    }
  };

  if (orders.length === 0) {
    return <p className="text-gray-500 text-sm py-8 text-center">No orders yet</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-xs text-gray-500 border-b border-gray-800">
            <th className="text-left pb-3 font-medium">Symbol</th>
            <th className="text-center pb-3 font-medium">Type</th>
            <th className="text-right pb-3 font-medium">Qty</th>
            <th className="text-right pb-3 font-medium">Price</th>
            <th className="text-right pb-3 font-medium">Total</th>
            <th className="text-center pb-3 font-medium">Status</th>
            <th className="text-left pb-3 font-medium">Bank</th>
            <th className="text-left pb-3 font-medium">Date</th>
            <th className="text-center pb-3 font-medium">Action</th>
          </tr>
        </thead>
        <tbody>
          {orders.map(order => (
            <tr key={order.id} className="border-b border-gray-800/40 hover:bg-gray-800/20">
              <td className="py-3">
                <div className="font-medium text-gray-200">{order.symbol.ticker}</div>
                {order.paperTrade && <div className="text-xs text-yellow-600">paper</div>}
              </td>
              <td className="text-center py-3"><OrderTypeBadge type={order.type} /></td>
              <td className="text-right font-mono text-gray-300">{order.quantity}</td>
              <td className="text-right font-mono text-gray-300">{order.price.toFixed(2)}</td>
              <td className="text-right font-mono text-gray-400">{(order.price * order.quantity).toFixed(2)}</td>
              <td className="text-center py-3"><OrderStatusBadge status={order.status} /></td>
              <td className="text-gray-500 text-xs capitalize">{order.bankAdapter}</td>
              <td className="text-gray-500 text-xs">
                {new Date(order.createdAt).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
              </td>
              <td className="text-center">
                {order.status === 'PENDING' && (
                  <button
                    onClick={() => handleCancel(order.id)}
                    className="text-xs text-red-400 hover:text-red-300 px-2 py-1 rounded hover:bg-red-500/10 transition-colors"
                  >
                    Cancel
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
