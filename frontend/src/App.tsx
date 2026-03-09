import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './stores/authStore';
import { useTradingStore } from './stores/tradingStore';
import { useBotStore } from './stores/botStore';
import { AppLayout } from './components/layout/AppLayout';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { OrdersPage } from './pages/OrdersPage';
import { PortfolioPage } from './pages/PortfolioPage';
import { SettingsPage } from './pages/SettingsPage';
import { LoadingSpinner } from './components/common/LoadingSpinner';
import { onBotTick, onBotStatus, onPriceUpdate } from './socket/socket';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore();
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

function AppInitializer({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuthStore();
  const { fetchSymbols, fetchOrders, fetchPortfolio, updateLivePrice, updateSignal } = useTradingStore();
  const { fetchStatus, addTick, setRunning } = useBotStore();

  useEffect(() => {
    if (!isAuthenticated) return;

    fetchSymbols();
    fetchOrders();
    fetchPortfolio();
    fetchStatus();

    const offTick = onBotTick(tick => {
      addTick(tick);
      updateSignal(tick.ticker, tick.signal);
    });

    const offStatus = onBotStatus(({ running }) => {
      setRunning(running);
    });

    const offPrice = onPriceUpdate(event => {
      updateLivePrice(event);
    });

    return () => {
      offTick();
      offStatus();
      offPrice();
    };
  }, [isAuthenticated]);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return <>{children}</>;
}

export default function App() {
  const { initFromStorage } = useAuthStore();

  useEffect(() => {
    initFromStorage();
  }, []);

  return (
    <BrowserRouter>
      <AppInitializer>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path="orders" element={<OrdersPage />} />
            <Route path="portfolio" element={<PortfolioPage />} />
            <Route path="settings" element={<SettingsPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AppInitializer>
    </BrowserRouter>
  );
}
