import { useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { importCsv } from '../api/endpoints';
import type { CsvImportResult } from '../types';
import { ApiError } from '../api/client';
import { RequireRole } from '../components/RequireRole';

function ImportForm() {
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<CsvImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    setFile(event.target.files?.[0] ?? null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!file) return;
    setSubmitting(true);
    setError(null);
    setResult(null);
    try {
      const importResult = await importCsv(file);
      setResult(importResult);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'CSV import failed.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      <h2>CSV Import</h2>
      <form onSubmit={handleSubmit} className="import-form">
        <input type="file" accept=".csv" onChange={handleFileChange} required />
        <button type="submit" disabled={!file || submitting}>
          {submitting ? 'Uploading…' : 'Import'}
        </button>
      </form>
      {error && <p className="form-error">{error}</p>}
      {result && (
        <div className="import-result" data-testid="import-result">
          <p>Rows imported: {result.rowsImported}</p>
          <p>Rows failed: {result.rowsFailed}</p>
          {result.errors && result.errors.length > 0 && (
            <ul>
              {result.errors.map((e, i) => (
                <li key={i}>{e}</li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}

export function ImportPage() {
  return (
    <RequireRole role="Engineer" fallback={<p>You do not have permission to import data.</p>}>
      <ImportForm />
    </RequireRole>
  );
}
