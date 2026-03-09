import { NavLink } from 'react-router-dom';

const NAV_ITEMS = [
  { to: '/', label: 'Dashboard', icon: '▣' },
  { to: '/orders', label: 'Orders', icon: '≡' },
  { to: '/portfolio', label: 'Portfolio', icon: '◎' },
  { to: '/settings', label: 'Settings', icon: '⚙' },
];

export function Sidebar() {
  return (
    <aside className="w-56 bg-gray-900 border-r border-gray-800 flex flex-col">
      <div className="p-4 border-b border-gray-800">
        <div className="text-blue-400 font-bold text-lg">🇹🇷 BIST Bot</div>
        <div className="text-gray-500 text-xs mt-0.5">Trading Dashboard</div>
      </div>

      <nav className="flex-1 p-2 space-y-1">
        {NAV_ITEMS.map(item => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-blue-600/20 text-blue-400 border border-blue-600/30'
                  : 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'
              }`
            }
          >
            <span className="text-base">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="p-3 border-t border-gray-800">
        <div className="text-xs text-gray-600 text-center">v1.0.0 — Paper Mode</div>
      </div>
    </aside>
  );
}
