import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ProductionChart } from './ProductionChart';
import type { ProductionRecord } from '../types';

describe('ProductionChart', () => {
  it('renders an empty state message when there is no data', () => {
    render(<ProductionChart data={[]} />);
    expect(screen.getByText('No production data available.')).toBeInTheDocument();
  });

  it('renders a chart container when production data is provided', () => {
    const data: ProductionRecord[] = [
      { date: '2026-01-01', oil: 100, gas: 200, water: 50 },
      { date: '2026-02-01', oil: 95, gas: 190, water: 55 },
    ];
    const { container } = render(<ProductionChart data={data} />);
    // Recharts renders a responsive container wrapper around the SVG chart.
    expect(container.querySelector('.recharts-responsive-container')).toBeTruthy();
  });
});
