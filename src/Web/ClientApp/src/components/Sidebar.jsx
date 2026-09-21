import { NavLink } from 'react-router-dom';
import { LayoutDashboard, Globe, BookOpen, Tags, Users } from 'lucide-react';

const menuItems = [
  { to: '/', label: 'Tổng quan', icon: LayoutDashboard, end: true },
  { to: '/web-sources', label: 'Nguồn web', icon: Globe },
  { to: '/story-sources', label: 'Nguồn truyện', icon: BookOpen },
  { to: '/categories', label: 'Thể loại', icon: Tags },
  { to: '/accounts', label: 'Tài khoản', icon: Users }
];

export function Sidebar() {
  return (
    <aside className="app-sidebar">
      <div className="app-brand">
        <BookOpen size={20} aria-hidden="true" />
        <span>Quản lý truyện</span>
      </div>

      <nav>
        <ul>
          {menuItems.map(({ to, label, icon: Icon, end }) => (
            <li key={to}>
              <NavLink to={to} end={end}>
                {({ isActive }) => (
                  <span className="app-nav-item" aria-current={isActive ? 'page' : undefined}>
                    <Icon size={18} aria-hidden="true" />
                    {label}
                  </span>
                )}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </aside>
  );
}
