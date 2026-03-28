import { useEffect, useState } from 'react';
import { api, type ProcessingLog } from '../api/client';

export default function Logs() {
  const [logs, setLogs] = useState<ProcessingLog[]>([]);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    api.getLogs(50).then(setLogs).finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  return (
    <div className="page">
      <div className="page-header">
        <h2>Processing Logs</h2>
        <button onClick={load} className="btn btn-secondary">Refresh</button>
      </div>

      <div className="card">
        {loading ? <div className="loading">Loading...</div> : logs.length === 0 ? (
          <p className="muted">No processing logs yet</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Started</th>
                <th>Completed</th>
                <th>Found</th>
                <th>Sent</th>
                <th>Failed</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(log => (
                <tr key={log.id} className={log.errorMessage ? 'row-error' : ''}>
                  <td>{log.id}</td>
                  <td>{new Date(log.startedAt).toLocaleString()}</td>
                  <td>{log.completedAt ? new Date(log.completedAt).toLocaleString() : 'Running...'}</td>
                  <td>{log.totalFound}</td>
                  <td>{log.totalSent}</td>
                  <td className={log.totalFailed > 0 ? 'text-danger' : ''}>{log.totalFailed}</td>
                  <td className="error-cell" title={log.errorMessage ?? ''}>
                    {log.errorMessage ? log.errorMessage.substring(0, 80) : '-'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
