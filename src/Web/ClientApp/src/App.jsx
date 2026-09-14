import AppRoutes from './AppRoutes';
import { AuthProvider } from './components/api-authorization/AuthContext';
import { ThemeProvider } from './components/ThemeContext';

export default function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </ThemeProvider>
  );
}
