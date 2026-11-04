import { useEffect, useState } from 'react';
import { absenceApi } from '../api/axios';

export default function Summary() {
  const [summary, setSummary] = useState([]);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const loadSummary = async (params = {}) => {
    const { data } = await absenceApi.get('/absences/summary', { params });
    setSummary(data);
  };

  useEffect(() => { loadSummary(); }, []);

  const handleFilter = (e) => {
    e.preventDefault();
    loadSummary({ startDate, endDate });
  };

  return (
    <div className="flex justify-center mt-10 px-4">
      <div className="w-full max-w-2xl bg-white rounded-lg shadow p-6 text-center">
        <h2 className="text-2xl font-bold mb-6">Summary Absen</h2>

        <form onSubmit={handleFilter} className="flex flex-wrap justify-center items-end gap-3 mb-6">
          <div className="text-left">
            <label className="block text-sm font-medium mb-1">From</label>
            <input
              type="date"
              className="border border-gray-300 rounded px-3 py-2"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
            />
          </div>
          <div className="text-left">
            <label className="block text-sm font-medium mb-1">To</label>
            <input
              type="date"
              className="border border-gray-300 rounded px-3 py-2"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
            />
          </div>
          <button
            type="submit"
            className="bg-blue-600 hover:bg-blue-700 text-white font-medium px-5 py-2 rounded-lg shadow transition"
          >
            Cari
          </button>
        </form>

        <table className="w-full border border-gray-300 mx-auto">
          <thead className="bg-gray-200">
            <tr>
              <th className="border border-gray-300 px-4 py-2">Date</th>
              <th className="border border-gray-300 px-4 py-2">Masuk</th>
              <th className="border border-gray-300 px-4 py-2">Pulang</th>
            </tr>
          </thead>
          <tbody>
            {summary.map((row) => (
              <tr key={row.date} className="hover:bg-gray-50">
                <td className="border border-gray-300 px-4 py-2">{row.date}</td>
                <td className="border border-gray-300 px-4 py-2">{row.masuk ?? '-'}</td>
                <td className="border border-gray-300 px-4 py-2">{row.pulang ?? '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}