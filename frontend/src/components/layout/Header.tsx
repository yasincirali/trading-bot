import { useAuthStore } from '../../stores/authStore';
import { useBotStore } from '../../stores/botStore';

export function Header() {
  const { user, logout } = useAuthStore();
  const { status } = useBotStore();

  return (
    <header className="h-14 bg-gray-900 border-b border-gray-800 flex items-center justify-between px-6">
      <div className="flex items-center gap-3">
        <div className={`flex items-center gap-2 text-sm font-medium ${status?.running ? 'text-green-400' : 'text-gray-500'}`}>
          <span className={`w-2 h-2 rounded-full ${status?.running ? 'bg-green-400 animate-pulse' : 'bg-gray-600'}`} />
          {status?.running ? 'Bot Running' : 'Bot Stopped'}
        </div>
        {status?.paperTrading && (
          <span className="text-xs bg-yellow-500/20 text-yellow-400 border border-yellow-500/30 px-2 py-0.5 rounded-full">
            PAPER TRADING
          </span>
        )}
      </div>

      <div className="flex items-center gap-4">
        <span className="text-sm text-gray-400">{user?.name ?? user?.email}</span>
        <button
          onClick={logout}
          className="text-xs text-gray-500 hover:text-gray-300 px-3 py-1.5 rounded-lg hover:bg-gray-800 transition-colors"
        >
          Logout
        </button>
      </div>
    </header>
  );
}
