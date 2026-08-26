import type { WellKpiSummary } from '../types';

interface KpiSummaryProps {
  kpi: WellKpiSummary;
}

/** Renders decline rate, production loss flag, and lift status for a well. */
export function KpiSummaryCard({ kpi }: KpiSummaryProps) {
  return (
    <div className="kpi-summary" data-testid="kpi-summary">
      <div className="kpi-item">
        <span className="kpi-label">Decline rate</span>
        <span className="kpi-value">{kpi.declineRatePct.toFixed(1)}%</span>
      </div>
      <div className="kpi-item">
        <span className="kpi-label">Production loss</span>
        <span className={`kpi-value ${kpi.productionLossFlag ? 'flag-bad' : 'flag-ok'}`}>
          {kpi.productionLossFlag ? 'Flagged' : 'Normal'}
        </span>
      </div>
      <div className="kpi-item">
        <span className="kpi-label">Artificial lift</span>
        <span className="kpi-value">{kpi.artificialLiftStatus}</span>
      </div>
    </div>
  );
}
