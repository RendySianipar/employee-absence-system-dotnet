import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { employeeApi } from '../api/axios';

export default function Login() {
  const [companyEmail, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    try {
      const { data } = await employeeApi.post('/auth/login', { companyEmail, password });
      if (data.employee.role !== 'admin') {
        setError('This account is not an admin.');
        return;
      }
      localStorage.setItem('admin_token', data.accessToken);
      localStorage.setItem('admin_employee', JSON.stringify(data.employee));
      navigate('/employees');
    } catch (err) {
      setError('Invalid email or password');
    }
  };

  return (
    <div className="flex justify-center mt-20 px-4">
      <div className="w-full max-w-sm bg-white rounded-lg shadow p-6">
        <h2 className="text-2xl font-bold mb-4 text-center">HRD Admin Login</h2>
        <form onSubmit={handleLogin} className="space-y-3">
          <input
            className="w-full border border-gray-300 rounded px-3 py-2"
            placeholder="Company Email"
            value={companyEmail}
            onChange={(e) => setEmail(e.target.value)}
          />
          <input
            className="w-full border border-gray-300 rounded px-3 py-2"
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          {error && <p className="text-red-600 text-sm">{error}</p>}
          <button className="w-full bg-blue-600 hover:bg-blue-700 text-white py-2 rounded-lg shadow transition" type="submit">
            Login
          </button>
        </form>
      </div>
    </div>
  );
}