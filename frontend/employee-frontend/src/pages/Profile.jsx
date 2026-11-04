import { useEffect, useState } from 'react';
import { employeeApi } from '../api/axios';

export default function Profile() {
  const [profile, setProfile] = useState(null);
  const [phoneNumber, setPhoneNumber] = useState('');
  const [message, setMessage] = useState('');
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    employeeApi.get('/employees/me').then((res) => {
      setProfile(res.data);
      setPhoneNumber(res.data.phoneNumber);
    });
  }, []);

  const handleUpdate = async (e) => {
    e.preventDefault();
    const { data } = await employeeApi.put('/employees/me', { phoneNumber });
    setProfile(data);
    setMessage('Profile updated!');
  };

  const handlePhotoChange = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    setUploading(true);
    try {
      const { data } = await employeeApi.post('/employees/me/photo', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      setProfile(data);
      setMessage('Photo updated!');
    } catch (err) {
      setMessage('Failed to upload photo');
    } finally {
      setUploading(false);
    }
  };

  if (!profile) {
    return <p className="text-center mt-10">Loading...</p>;
  }

  return (
    <div className="flex justify-center mt-10 px-4">
      <div className="w-full max-w-2xl bg-white rounded-lg shadow p-6 text-center">
        <h2 className="text-2xl font-bold mb-6">My Profile</h2>

        <div className="flex flex-col items-center mb-6">
          {profile.photoUrl ? (
            <img
              src={profile.photoUrl}
              alt="Profile"
              className="w-28 h-28 rounded-full object-cover border-4 border-gray-200 mb-3"
            />
          ) : (
            <div className="w-28 h-28 rounded-full bg-gray-200 flex items-center justify-center text-gray-500 border-4 border-gray-200 mb-3">
              No Photo
            </div>
          )}

          <label className="cursor-pointer text-blue-600 hover:underline text-sm">
            {uploading ? 'Uploading...' : 'Change Photo'}
            <input type="file" accept="image/*" className="hidden" onChange={handlePhotoChange} disabled={uploading} />
          </label>
        </div>

        <form onSubmit={handleUpdate} className="text-left space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input className="w-full border border-gray-300 rounded px-3 py-2 bg-gray-100 text-gray-600" value={profile.name} disabled />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Company Email</label>
            <input className="w-full border border-gray-300 rounded px-3 py-2 bg-gray-100 text-gray-600" value={profile.companyEmail} disabled />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Position</label>
            <input className="w-full border border-gray-300 rounded px-3 py-2 bg-gray-100 text-gray-600" value={profile.position} disabled />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Phone Number</label>
            <input className="w-full border border-gray-300 rounded px-3 py-2" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} />
          </div>
          <div className="text-center pt-2">
            <button type="submit" className="bg-blue-600 hover:bg-blue-700 text-white font-medium px-6 py-2 rounded-lg shadow transition">
              Save
            </button>
          </div>
          {message && <p className="text-green-600 text-sm text-center">{message}</p>}
        </form>
      </div>
    </div>
  );
}