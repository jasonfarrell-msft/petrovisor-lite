import { useEffect, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts';
import { getDashboardSummary } from '../api/endpoints';
import type { DashboardSummary } from '../types';
import { ApiError } from '../api/client';

export function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    getDashboardSummary()
      .then((data) => {
        if (!cancelled) setSummary(data);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Failed to load dashboard summary.');
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) return <p>Loading dashboard…</p>;
  if (error) return <p className="form-error">{error}</p>;
  if (!summary) return null;

  const chartData = [
    { name: 'Oil', volume: summary.totalOilVolume },
    { name: 'Gas', volume: summary.totalGasVolume },
    { name: 'Water', volume: summary.totalWaterVolume },
  ];

  return (
    <div>
      <h2>Field Summary</h2>
      <div className="card-grid">
        <div className="card">
          <h3>{summary.wellCount}</h3>
          <p>Total wells</p>
        </div>
        <div className="card">
          <h3>{summary.wellsWithLossFlags}</h3>
          <p>Wells with loss flags</p>
        </div>
        <div className="card">
          <h3>{summary.wellsWithLiftIssues}</h3>
          <p>Wells with lift issues</p>
        </div>
        <div className="card">
          <h3>{summary.totalOilVolume.toLocaleString()}</h3>
          <p>Total oil volume</p>
        </div>
      </div>

      <h3>Aggregate volumes</h3>
      <ResponsiveContainer width="100%" height={280}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="name" />
          <YAxis />
          <Tooltip />
          <Bar dataKey="volume" fill="#1565c0" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
