import { api } from './client';
import type {
  Well,
  ProductionRecord,
  WellKpiSummary,
  DashboardSummary,
  LoginResponse,
  CsvImportResult,
} from '../types';

export function login(username: string, password: string): Promise<LoginResponse> {
  return api.post<LoginResponse>('/api/auth/login', { username, password }, { auth: false });
}

export function getWells(): Promise<Well[]> {
  return api.get<Well[]>('/api/wells');
}

export function getWell(id: string): Promise<Well> {
  return api.get<Well>(`/api/wells/${encodeURIComponent(id)}`);
}

export function getProductionForWell(wellId: string): Promise<ProductionRecord[]> {
  return api.get<ProductionRecord[]>(`/api/production?wellId=${encodeURIComponent(wellId)}`);
}

export function getWellKpi(wellId: string): Promise<WellKpiSummary> {
  return api.get<WellKpiSummary>(`/api/kpi/wells/${encodeURIComponent(wellId)}`);
}

export function getDashboardSummary(): Promise<DashboardSummary> {
  return api.get<DashboardSummary>('/api/kpi/dashboard');
}

export function importCsv(file: File): Promise<CsvImportResult> {
  const formData = new FormData();
  formData.append('file', file);
  return api.post<CsvImportResult>('/api/import/csv', formData);
}
