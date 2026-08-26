import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { getWell, getProductionForWell, getWellKpi } from '../api/endpoints';
import type { Well, ProductionRecord, WellKpiSummary } from '../types';
import { ProductionChart } from '../components/ProductionChart';
import { KpiSummaryCard } from '../components/KpiSummaryCard';
import { ApiError } from '../api/client';

export function WellDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [well, setWell] = useState<Well | null>(null);
  const [production, setProduction] = useState<ProductionRecord[]>([]);
  const [kpi, setKpi] = useState<WellKpiSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    setLoading(true);
    Promise.all([getWell(id), getProductionForWell(id), getWellKpi(id)])
      .then(([wellData, productionData, kpiData]) => {
        if (cancelled) return;
        setWell(wellData);
        setProduction(productionData);
        setKpi(kpiData);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Failed to load well details.');
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (loading) return <p>Loading well…</p>;
  if (error) return <p className="form-error">{error}</p>;

  return (
    <div>
      <h2>{well?.name ?? id}</h2>
      {well && (
        <p>
          Facility: {well.facility} · Status: {well.status}
        </p>
      )}

      {kpi && <KpiSummaryCard kpi={kpi} />}

      <h3>Production trend</h3>
      <ProductionChart data={production} />
    </div>
  );
}
