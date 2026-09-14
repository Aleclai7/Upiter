import { useState, useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import "./App.css";

const API_BASE = "http://localhost:5018";

function App() {
  const [ targets, setTargets ] = useState([]);
  const [ loading, setLoading ] = useState(true);
  const [ error, setError ] = useState(null);
  
  useEffect(() => {
    fetch(`${API_BASE}/targets`)
    .then((res) => {
      if (!res.ok) {
        throw new Error("Network response was not ok");
      }
      return res.json();
    })
    .then((data) => {
      setTargets(data);
      setLoading(false);
    })
    .catch((err) => {
      setError(err.message);
      setLoading(false);
    });
  }), [];

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/status`)
      .withAutomaticReconnect()
      .build();

    let isMounted = true;

    connection.on('StatusChanged', (data) => {
      console.log('StatusChanged received:', data);
      setTargets((prev) =>
        prev.map((t) =>
          t.id === data.targetId
            ? { ...t, lastKnownUp: data.isUp, lastCheckedAt: data.checkedAt }
            : t
        )
      );
    });

    connection
      .start()
      .then(() => {
        if (isMounted) console.log('Connected to StatusHub');
      })
      .catch((err) => {
        if (isMounted) console.error('SignalR connection failed:', err);
      });

    return () => {
      isMounted = false;
      connection.stop();
    };
  }, []);

    if (loading)
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-950 text-gray-300">
        Loading targets...
      </div>
    );

  if (error)
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-950 text-red-400">
        Error: {error}
      </div>
    );

  return (
        <div className="min-h-screen bg-gray-950 text-gray-100 px-4 py-10">
      <div className="max-w-3xl mx-auto">
        <h1 className="text-3xl font-bold mb-1">Upiter</h1>
        <p className="text-gray-400 mb-6">
          Monitoring {targets.length} target(s)
        </p>

        <div className="overflow-hidden rounded-lg border border-gray-800">
          <table className="w-full text-left">
            <thead className="bg-gray-900 text-gray-400 text-sm uppercase">
              <tr>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">URL</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Last Checked</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-800">
              {targets.map((t) => (
                <tr key={t.id} className="hover:bg-gray-900/50">
                  <td className="px-4 py-3 font-medium">{t.name}</td>
                  <td className="px-4 py-3 text-gray-400">{t.url}</td>
                  <td className="px-4 py-3">
                    <span
                      className={`inline-flex items-center gap-1.5 px-2 py-1 rounded-full text-xs font-semibold ${
                        t.lastKnownUp
                          ? 'bg-green-500/10 text-green-400'
                          : t.lastKnownUp === false
                          ? 'bg-red-500/10 text-red-400'
                          : 'bg-gray-500/10 text-gray-400'
                      }`}
                    >
                      <span
                        className={`w-1.5 h-1.5 rounded-full ${
                          t.lastKnownUp
                            ? 'bg-green-400'
                            : t.lastKnownUp === false
                            ? 'bg-red-400'
                            : 'bg-gray-400'
                        }`}
                      />
                      {t.lastKnownUp === null
                        ? 'Unknown'
                        : t.lastKnownUp
                        ? 'UP'
                        : 'DOWN'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-400 text-sm">
                    {t.lastCheckedAt
                      ? new Date(t.lastCheckedAt).toLocaleTimeString()
                      : 'Never'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default App;