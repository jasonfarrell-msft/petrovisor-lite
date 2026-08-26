// Shared domain types for PetroVisor Lite frontend.
// NOTE: these are Luke's typed assumptions about Han's REST API shapes.
// Adjust field names/types once Han's backend contracts are finalized.

export type UserRole = 'Engineer' | 'Viewer';

export interface AuthUser {
  username: string;
  role: UserRole;
}

export interface LoginResponse {
  token: string;
}

export interface Well {
  id: string;
  name: string;
  facility: string;
  status: string;
}

export interface ProductionRecord {
  date: string; // ISO date string
  oil: number;
  gas: number;
  water: number;
}

export interface WellKpiSummary {
  wellId: string;
  declineRatePct: number;
  productionLossFlag: boolean;
  artificialLiftStatus: string;
}

export interface DashboardSummary {
  totalOilVolume: number;
  totalGasVolume: number;
  totalWaterVolume: number;
  wellCount: number;
  wellsWithLossFlags: number;
  wellsWithLiftIssues: number;
}

export interface CsvImportResult {
  rowsImported: number;
  rowsFailed: number;
  errors?: string[];
}
