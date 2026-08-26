import { Routes, Route } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { Layout } from './components/Layout';
import { LoginPage } from './pages/LoginPage';
import { WellListPage } from './pages/WellListPage';
import { WellDetailPage } from './pages/WellDetailPage';
import { DashboardPage } from './pages/DashboardPage';
import { ImportPage } from './pages/ImportPage';

export function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path="wells" element={<WellListPage />} />
          <Route path="wells/:id" element={<WellDetailPage />} />
          <Route path="import" element={<ImportPage />} />
        </Route>
      </Routes>
    </AuthProvider>
  );
}
