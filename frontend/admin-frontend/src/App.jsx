import { BrowserRouter, Routes, Route, Link, Navigate, useNavigate, useLocation } from 'react-router-dom';
import Login from './pages/Login';
import Employees from './pages/Employees';
import Attendance from './pages/Attendance';
import NotificationBell from './components/NotificationBell';

function PrivateRoute({ children }) {
  return localStorage.getItem('admin_token') ? children : <Navigate to="/login" />;
}

function Nav() {
  const navigate = useNavigate();
  const location = useLocation();

  if (location.pathname === '/login') return null;
  if (!localStorage.getItem('admin_token')) return null;

  const handleLogout = () => {
    localStorage.removeItem('admin_token');
    localStorage.removeItem('admin_employee');
    navigate('/login');
  };

  return (
    <nav className="bg-white shadow px-6 py-4 flex justify-between items-center">
      <div className="flex gap-6 items-center">
        <Link className="text-blue-600 font-medium hover:underline" to="/employees">Employees</Link>
        <Link className="text-blue-600 font-medium hover:underline" to="/attendance">Attendance</Link>
        <NotificationBell />
      </div>
      <button onClick={handleLogout} className="text-red-600 font-medium hover:underline">
        Sign Out
      </button>
    </nav>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Nav />
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/employees" element={<PrivateRoute><Employees /></PrivateRoute>} />
        <Route path="/attendance" element={<PrivateRoute><Attendance /></PrivateRoute>} />
        <Route path="*" element={<Navigate to="/login" />} />
      </Routes>
    </BrowserRouter>
  );
}