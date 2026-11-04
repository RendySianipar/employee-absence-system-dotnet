import { useState } from 'react';
import { absenceApi } from '../api/axios';

export default function Absen() {
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  // The API takes the employee from the JWT and the date/time from the server clock.
  const handleAbsen = async (status) => {
    setMessage('');
    setError('');
    try {
      const { data } = await absenceApi.post('/absences', { status });
      setMessage(`${status === 'masuk' ? 'Check In' : 'Check Out'} recorded at ${data.time} (${data.date})`);
    } catch (err) {
      setError(err.response?.data?.title ?? 'Failed to record attendance');
    }
  };

  return (
    <div className="flex justify-center mt-10 px-4">
      <div className="w-full max-w-md bg-white rounded-lg shadow p-6 text-center">
        <h2 className="text-2xl font-bold mb-6">Absen</h2>

        <div className="flex justify-center gap-4">
          <button
            className="bg-green-600 hover:bg-green-700 text-white font-medium px-6 py-2 rounded-lg shadow transition"
            onClick={() => handleAbsen('masuk')}
          >
            Check In (Masuk)
          </button>
          <button
            className="bg-yellow-500 hover:bg-yellow-600 text-white font-medium px-6 py-2 rounded-lg shadow transition"
            onClick={() => handleAbsen('pulang')}
          >
            Check Out (Pulang)
          </button>
        </div>

        {message && <p className="text-green-600 text-sm mt-4">{message}</p>}
        {error && <p className="text-red-600 text-sm mt-4">{error}</p>}
      </div>
    </div>
  );
}