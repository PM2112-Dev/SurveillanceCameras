import { NavLink } from 'react-router-dom';
import {
  BookOpen,
  Bot,
  FileText,
  Globe,
  LayoutDashboard,
  MessageSquareText,
  Tags,
  UserCog,
  Users,
  Library,
  Clapperboard,
  Contact,
  Shapes,
  Film
} from 'lucide-react';

const menuSections = [
  {
    items: [{ to: '/', label: 'Tổng quan', icon: LayoutDashboard, end: true }]
  },
  {
    heading: 'Nội dung',
    items: [
      { to: '/web-sources', label: 'Nguồn web', icon: Globe },
      { to: '/story-sources', label: 'Nguồn truyện', icon: BookOpen },
      { to: '/categories', label: 'Thể loại', icon: Tags },
      { to: '/stories', label: 'Truyện', icon: Library },
      { to: '/movies', label: 'Phim', icon: Film },
      { to: '/story-video-types', label: 'Loại video truyện', icon: Clapperboard }
    ]
  },
  {
    heading: 'Dịch bằng AI',
    items: [
      { to: '/ais', label: 'AI', icon: Bot },
      { to: '/prompt-types', label: 'Loại prompt', icon: FileText },
      { to: '/prompts', label: 'Prompt', icon: MessageSquareText }
    ]
  },
  {
    heading: 'Tài khoản',
    items: [
      { to: '/accounts', label: 'Tài khoản', icon: Users },
      { to: '/account-types', label: 'Loại tài khoản', icon: UserCog }
    ]
  },
  {
    heading: 'Kênh mạng xã hội',
    items: [
      { to: '/user-profiles', label: 'Kênh đã lưu', icon: Contact },
      { to: '/user-profile-types', label: 'Loại kênh', icon: Shapes }
    ]
  }
];

export function Sidebar() {
  return (
    <aside className="app-sidebar">
      <div className="app-brand">
        <BookOpen size={20} aria-hidden="true" />
        <span>Quản lý truyện</span>
      </div>

      <nav>
        {menuSections.map((section, index) => (
          <ul key={section.heading ?? index}>
            {section.heading && <li className="app-nav-heading">{section.heading}</li>}
            {section.items.map(({ to, label, icon: Icon, end }) => (
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
        ))}
      </nav>
    </aside>
  );
}
