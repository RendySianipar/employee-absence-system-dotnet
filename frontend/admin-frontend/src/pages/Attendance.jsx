import { useEffect, useState } from 'react';
import { absenceApi, employeeApi } from '../api/axios';

export default function Attendance() {
  const [records, setRecords] = useState([]);
  const [employeeMap, setEmployeeMap] = useState({});
  const [message, setMessage] = useState('');

  useEffect(() => {
    const loadData = async () => {
      try {
        const [attendanceRes, employeesRes] = await Promise.all([
          absenceApi.get('/absences/admin'),
          employeeApi.get('/employees'),
        ]);

        const map = {};
        employeesRes.data.forEach((emp) => {
          map[emp.id] = emp.name;
        });

        setEmployeeMap(map);
        setRecords(attendanceRes.data);
      } catch (err) {
        setMessage('Failed to load attendance');
      }
    };

    loadData();
  }, []);

  return (
    <div className="flex justify-center mt-10 px-4">
      <div className="w-full max-w-3xl bg-white rounded-lg shadow p-6">
        <h2 className="text-2xl font-bold mb-6 text-center">All Employee Attendance (Read Only)</h2>

        {message && <p className="text-center text-sm text-red-600 mb-4">{message}</p>}

        <table className="w-full border border-gray-300">
          <thead className="bg-gray-200">
            <tr>
              <th className="border border-gray-300 px-3 py-2">Employee Name</th>
              <th className="border border-gray-300 px-3 py-2">Date</th>
              <th className="border border-gray-300 px-3 py-2">Time</th>
              <th className="border border-gray-300 px-3 py-2">Status</th>
            </tr>
          </thead>
          <tbody>
            {records.map((r) => (
              <tr key={r.id} className="hover:bg-gray-50">
                <td className="border border-gray-300 px-3 py-2">{employeeMap[r.employeeId] ?? r.employeeId}</td>
                <td className="border border-gray-300 px-3 py-2">{r.date}</td>
                <td className="border border-gray-300 px-3 py-2">{r.time}</td>
                <td className="border border-gray-300 px-3 py-2 capitalize">{r.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}