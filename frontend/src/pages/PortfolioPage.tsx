import { PnlSummary } from '../components/portfolio/PnlSummary';
import { PositionTable } from '../components/portfolio/PositionTable';

export function PortfolioPage() {
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold text-gray-100">Portfolio</h1>
        <p className="text-gray-500 text-sm mt-0.5">Open positions & P&L</p>
      </div>

      <PnlSummary />

      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-4">Open Positions</h2>
        <PositionTable />
      </div>
    </div>
  );
}
