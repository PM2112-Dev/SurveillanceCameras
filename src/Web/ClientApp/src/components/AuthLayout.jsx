import { Outlet } from 'react-router-dom';
import { ThemeToggle } from './ThemeToggle';

export function AuthLayout() {
  return (
    <div className="auth-shell">
      <div className="auth-shell-toolbar">
        <ThemeToggle />
      </div>

      <main className="auth-card">
        <Outlet />
      </main>
    </div>
  );
}
