import { Navigate, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
  const { user, isAuthenticated, logout } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="app-layout">
      <header className="app-header">
        <h1>PetroVisor Lite</h1>
        <nav>
          <NavLink to="/" end>
            Dashboard
          </NavLink>
          <NavLink to="/wells">Wells</NavLink>
          {user?.role === 'Engineer' && <NavLink to="/import">CSV Import</NavLink>}
        </nav>
        <div className="app-user">
          <span>
            {user?.username} ({user?.role})
          </span>
          <button type="button" onClick={logout}>
            Logout
          </button>
        </div>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
