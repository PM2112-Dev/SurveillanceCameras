import { Outlet, useNavigate } from 'react-router-dom';
import { LogOut } from 'lucide-react';
import { Sidebar } from './Sidebar';
import { ThemeToggle } from './ThemeToggle';
import { useAuth } from './api-authorization/AuthContext';

export function AppLayout() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="app-shell">
      <Sidebar />

      <div className="app-content">
        <header className="app-topbar">
          <ThemeToggle />
          <button type="button" className="icon-btn" onClick={handleLogout} title="Đăng xuất">
            <LogOut size={18} aria-hidden="true" />
          </button>
        </header>

        <main>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
