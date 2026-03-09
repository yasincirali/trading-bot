import { ManualOrderForm } from '../components/orders/ManualOrderForm';
import { OrderTable } from '../components/orders/OrderTable';

export function OrdersPage() {
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold text-gray-100">Orders</h1>
        <p className="text-gray-500 text-sm mt-0.5">Place and manage your orders</p>
      </div>

      <ManualOrderForm />

      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-4">Order History</h2>
        <OrderTable />
      </div>
    </div>
  );
}
