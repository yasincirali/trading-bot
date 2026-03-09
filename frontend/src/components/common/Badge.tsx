import { SignalType, OrderStatus, OrderType } from '../../types';

interface BadgeProps {
  children: React.ReactNode;
  variant?: 'buy' | 'sell' | 'neutral' | 'success' | 'error' | 'warning' | 'info';
  size?: 'sm' | 'md';
}

export function Badge({ children, variant = 'info', size = 'sm' }: BadgeProps) {
  const colors: Record<string, string> = {
    buy: 'bg-green-500/20 text-green-400 border border-green-500/30',
    sell: 'bg-red-500/20 text-red-400 border border-red-500/30',
    neutral: 'bg-gray-500/20 text-gray-400 border border-gray-500/30',
    success: 'bg-green-500/20 text-green-400 border border-green-500/30',
    error: 'bg-red-500/20 text-red-400 border border-red-500/30',
    warning: 'bg-yellow-500/20 text-yellow-400 border border-yellow-500/30',
    info: 'bg-blue-500/20 text-blue-400 border border-blue-500/30',
  };

  const sizes: Record<string, string> = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-3 py-1 text-sm',
  };

  return (
    <span className={`inline-flex items-center rounded-full font-medium ${colors[variant]} ${sizes[size]}`}>
      {children}
    </span>
  );
}

export function SignalBadge({ signal }: { signal: SignalType }) {
  const variant = signal === 'BUY' ? 'buy' : signal === 'SELL' ? 'sell' : 'neutral';
  return <Badge variant={variant}>{signal}</Badge>;
}

export function OrderTypeBadge({ type }: { type: OrderType }) {
  return <Badge variant={type === 'BUY' ? 'buy' : 'sell'}>{type}</Badge>;
}

export function OrderStatusBadge({ status }: { status: OrderStatus }) {
  const variant =
    status === 'FILLED' ? 'success' :
    status === 'PENDING' ? 'warning' :
    status === 'CANCELLED' ? 'neutral' : 'error';
  return <Badge variant={variant}>{status}</Badge>;
}
