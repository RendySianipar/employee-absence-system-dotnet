import { useEffect, useState } from 'react';
import { employeeApi } from '../api/axios';

export default function Employees() {
  const [employees, setEmployees] = useState([]);
  const [form, setForm] = useState({ name: '', companyEmail: '', password: '', position: '', phoneNumber: '', role: 'employee' });
  const [message, setMessage] = useState('');

  const loadEmployees = async () => {
    try {
      const { data } = await employeeApi.get('/employees');
      setEmployees(data);
    } catch (err) {
      setMessage('Failed to load employees');
    }
  };

  useEffect(() => { loadEmployees(); }, []);

  const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

  const handleCreate = async (e) => {
    e.preventDefault();
    try {
      await employeeApi.post('/employees', form);
      setMessage('Employee created!');
      setForm({ name: '', companyEmail: '', password: '', position: '', phoneNumber: '', role: 'employee' });
      loadEmployees();
    } catch (err) {
      setMessage('Failed to create employee');
    }
  };

  return (
    <div className="flex justify-center mt-10 px-4">
      <div className="w-full max-w-3xl bg-white rounded-lg shadow p-6">
        <h2 className="text-2xl font-bold mb-6 text-center">Manage Employees</h2>

        <form onSubmit={handleCreate} className="grid grid-cols-2 gap-3 mb-6">
          <input className="border border-gray-300 rounded px-3 py-2" name="name" placeholder="Name" value={form.name} onChange={handleChange} required />
          <input className="border border-gray-300 rounded px-3 py-2" name="companyEmail" placeholder="Company Email" value={form.companyEmail} onChange={handleChange} required />
          <input className="border border-gray-300 rounded px-3 py-2" name="password" type="password" placeholder="Password" value={form.password} onChange={handleChange} required />
          <input className="border border-gray-300 rounded px-3 py-2" name="position" placeholder="Position" value={form.position} onChange={handleChange} required />
          <input className="border border-gray-300 rounded px-3 py-2" name="phoneNumber" placeholder="Phone Number" value={form.phoneNumber} onChange={handleChange} required />
          <select className="border border-gray-300 rounded px-3 py-2" name="role" value={form.role} onChange={handleChange}>
            <option value="employee">Employee</option>
            <option value="admin">Admin</option>
          </select>
          <button className="col-span-2 bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 rounded-lg shadow transition" type="submit">
            Add Employee
          </button>
        </form>

        {message && <p className="text-center text-sm text-green-600 mb-4">{message}</p>}

        <table className="w-full border border-gray-300">
          <thead className="bg-gray-200">
            <tr>
              <th className="border border-gray-300 px-3 py-2">Name</th>
              <th className="border border-gray-300 px-3 py-2">Email</th>
              <th className="border border-gray-300 px-3 py-2">Position</th>
              <th className="border border-gray-300 px-3 py-2">Role</th>
            </tr>
          </thead>
          <tbody>
            {employees.map((emp) => (
              <tr key={emp.id} className="hover:bg-gray-50">
                <td className="border border-gray-300 px-3 py-2">{emp.name}</td>
                <td className="border border-gray-300 px-3 py-2">{emp.companyEmail}</td>
                <td className="border border-gray-300 px-3 py-2">{emp.position}</td>
                <td className="border border-gray-300 px-3 py-2 capitalize">{emp.role}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}