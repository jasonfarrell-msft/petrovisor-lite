import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';
import type { ProductionRecord } from '../types';

interface ProductionChartProps {
  data: ProductionRecord[];
}

/** Time series line chart of oil/gas/water volumes for a well. */
export function ProductionChart({ data }: ProductionChartProps) {
  if (data.length === 0) {
    return <p>No production data available.</p>;
  }

  return (
    <ResponsiveContainer width="100%" height={320}>
      <LineChart data={data} margin={{ top: 8, right: 24, bottom: 8, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" />
        <YAxis />
        <Tooltip />
        <Legend />
        <Line type="monotone" dataKey="oil" stroke="#2e7d32" name="Oil" dot={false} />
        <Line type="monotone" dataKey="gas" stroke="#c62828" name="Gas" dot={false} />
        <Line type="monotone" dataKey="water" stroke="#1565c0" name="Water" dot={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}
