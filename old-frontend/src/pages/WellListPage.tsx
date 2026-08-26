import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getWells } from '../api/endpoints';
import type { Well } from '../types';
import { ApiError } from '../api/client';

export function WellListPage() {
  const [wells, setWells] = useState<Well[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    getWells()
      .then((data) => {
        if (!cancelled) setWells(data);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Failed to load wells.');
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) return <p>Loading wells…</p>;
  if (error) return <p className="form-error">{error}</p>;

  return (
    <div>
      <h2>Wells</h2>
      <table className="data-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Facility</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {wells.map((well) => (
            <tr key={well.id}>
              <td>{well.id}</td>
              <td>
                <Link to={`/wells/${well.id}`}>{well.name}</Link>
              </td>
              <td>{well.facility}</td>
              <td>{well.status}</td>
            </tr>
          ))}
          {wells.length === 0 && (
            <tr>
              <td colSpan={4}>No wells found.</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
