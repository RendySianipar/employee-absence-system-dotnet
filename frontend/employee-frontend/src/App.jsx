import { BrowserRouter, Routes, Route, Link, Navigate, useNavigate, useLocation } from 'react-router-dom';
import Login from './pages/Login';
import Profile from './pages/Profile';
import Absen from './pages/Absen';
import Summary from './pages/Summary';

function PrivateRoute({ children }) {
  return localStorage.getItem('token') ? children : <Navigate to="/login" />;
}

function Nav() {
  const navigate = useNavigate();
  const location = useLocation();

  if (location.pathname === '/login') return null;
  if (!localStorage.getItem('token')) return null;

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('employee');
    navigate('/login');
  };

  return (
    <nav className="bg-white shadow px-6 py-4 flex justify-between items-center">
      <div className="flex gap-6">
        <Link className="text-blue-600 font-medium hover:underline" to="/profile">Profile</Link>
        <Link className="text-blue-600 font-medium hover:underline" to="/absen">Absen</Link>
        <Link className="text-blue-600 font-medium hover:underline" to="/summary">Summary</Link>
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
        <Route path="/profile" element={<PrivateRoute><Profile /></PrivateRoute>} />
        <Route path="/absen" element={<PrivateRoute><Absen /></PrivateRoute>} />
        <Route path="/summary" element={<PrivateRoute><Summary /></PrivateRoute>} />
        <Route path="*" element={<Navigate to="/login" />} />
      </Routes>
    </BrowserRouter>
  );
}