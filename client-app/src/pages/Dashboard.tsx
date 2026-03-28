import { useEffect, useState } from 'react';
import { api, type DashboardData } from '../api/client';

export default function Dashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);

  const load = () => {
    setLoading(true);
    api.getDashboard().then(setData).finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const triggerProcess = async () => {
    setProcessing(true);
    try {
      await api.triggerProcessing();
      load();
    } finally {
      setProcessing(false);
    }
  };

  if (loading) return <div className="loading">Loading...</div>;
  if (!data) return <div className="error">Failed to load dashboard</div>;

  return (
    <div className="page">
      <div className="page-header">
        <h2>Dashboard</h2>
        <div className="actions">
          <button onClick={load} className="btn btn-secondary">Refresh</button>
          <button onClick={triggerProcess} disabled={processing} className="btn btn-primary">
            {processing ? 'Processing...' : 'Run Now'}
          </button>
        </div>
      </div>

      <div className="stats-grid">
        <div className="stat-card stat-pending">
          <div className="stat-value">{data.totalPending}</div>
          <div className="stat-label">Pending</div>
        </div>
        <div className="stat-card stat-sent">
          <div className="stat-value">{data.totalSent}</div>
          <div className="stat-label">Sent</div>
        </div>
        <div className="stat-card stat-failed">
          <div className="stat-value">{data.totalFailed}</div>
          <div className="stat-label">Failed</div>
        </div>
        <div className="stat-card stat-today">
          <div className="stat-value">{data.todayCount}</div>
          <div className="stat-label">Today</div>
        </div>
      </div>

      {data.countries.length > 0 && (
        <div className="card">
          <h3>Active Countries</h3>
          <div className="tag-list">
            {data.countries.map(c => <span key={c} className="tag">{c}</span>)}
          </div>
        </div>
      )}

      <div className="card">
        <h3>Recent Processing Runs</h3>
        {data.recentLogs.length === 0 ? (
          <p className="muted">No processing runs yet</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Started</th>
                <th>Duration</th>
                <th>Found</th>
                <th>Sent</th>
                <th>Failed</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {data.recentLogs.map(log => (
                <tr key={log.id}>
                  <td>{new Date(log.startedAt).toLocaleString()}</td>
                  <td>{log.completedAt
                    ? `${Math.round((new Date(log.completedAt).getTime() - new Date(log.startedAt).getTime()) / 1000)}s`
                    : 'Running...'}</td>
                  <td>{log.totalFound}</td>
                  <td>{log.totalSent}</td>
                  <td className={log.totalFailed > 0 ? 'text-danger' : ''}>{log.totalFailed}</td>
                  <td>
                    {log.errorMessage
                      ? <span className="badge badge-danger">Error</span>
                      : log.completedAt
                        ? <span className="badge badge-success">Complete</span>
                        : <span className="badge badge-warning">Running</span>}
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
