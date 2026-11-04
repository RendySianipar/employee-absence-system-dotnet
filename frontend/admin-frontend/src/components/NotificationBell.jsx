import { useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

// SignalR hub on Employee Service (.NET). Only admins can connect: the JWT is sent as access_token.
const HUB_URL = 'http://localhost:3000/hubs/notifications';

export default function NotificationBell() {
  const [notifications, setNotifications] = useState([]);
  const [open, setOpen] = useState(false);
  const [unseen, setUnseen] = useState(0);

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => localStorage.getItem('admin_token') ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('profile_updated', (data) => {
      setNotifications((prev) => [data, ...prev].slice(0, 10));
      setUnseen((n) => n + 1);
    });

    connection.start().catch((err) => console.error('SignalR connection failed', err));

    return () => {
      connection.stop();
    };
  }, []);

  const handleOpen = () => {
    setOpen(!open);
    setUnseen(0);
  };

  return (
    <div className="relative inline-block">
      <button
        onClick={handleOpen}
        className="relative text-2xl w-10 h-10 flex items-center justify-center rounded-full hover:bg-gray-100 transition"
      >
        🔔
        {unseen > 0 && (
          <span className="absolute top-0 right-0 bg-red-600 text-white text-[10px] font-bold rounded-full w-5 h-5 flex items-center justify-center border-2 border-white">
            {unseen}
          </span>
        )}
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />

          <div className="absolute left-1/2 -translate-x-1/2 top-12 w-72 bg-white border border-gray-200 rounded-lg shadow-xl z-20 overflow-hidden">
            <div className="px-4 py-2.5 border-b bg-gray-50 flex justify-between items-center">
              <span className="font-semibold text-sm text-gray-700">Notifications</span>
              <button onClick={() => setOpen(false)} className="text-gray-400 hover:text-gray-600 text-sm leading-none">
                ✕
              </button>
            </div>

            <div className="max-h-72 overflow-y-auto divide-y divide-gray-100">
              {notifications.length === 0 && (
                <p className="px-4 py-6 text-xs text-gray-400 text-center">No notifications yet</p>
              )}
              {notifications.map((n, i) => (
                <div key={i} className="px-4 py-3 hover:bg-gray-50 transition">
                  <p className="text-xs font-semibold text-gray-800 mb-0.5">
                    {n.employeeName} updated their profile
                  </p>
                  <div className="text-xs text-gray-600">
                    {Object.entries(n.changedFields || {}).map(([k, v]) => (
                      <div key={k}>{k}: {v}</div>
                    ))}
                  </div>
                  <p className="text-[10px] text-gray-400 mt-1">
                    {new Date(n.timestamp).toLocaleString()}
                  </p>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
}