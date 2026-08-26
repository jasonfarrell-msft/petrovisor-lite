import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { WellListPage } from './WellListPage';
import * as endpoints from '../api/endpoints';
import type { Well } from '../types';

describe('WellListPage', () => {
  it('renders a table row for each well returned by the API', async () => {
    const wells: Well[] = [
      { id: 'w1', name: 'Well One', facility: 'Facility A', status: 'Active' },
      { id: 'w2', name: 'Well Two', facility: 'Facility B', status: 'Shut-in' },
    ];
    vi.spyOn(endpoints, 'getWells').mockResolvedValue(wells);

    render(
      <MemoryRouter>
        <WellListPage />
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getByText('Well One')).toBeInTheDocument());
    expect(screen.getByText('Well Two')).toBeInTheDocument();
    expect(screen.getByText('Facility A')).toBeInTheDocument();
    expect(screen.getByText('Shut-in')).toBeInTheDocument();
  });

  it('shows an empty state when there are no wells', async () => {
    vi.spyOn(endpoints, 'getWells').mockResolvedValue([]);

    render(
      <MemoryRouter>
        <WellListPage />
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getByText('No wells found.')).toBeInTheDocument());
  });
});
