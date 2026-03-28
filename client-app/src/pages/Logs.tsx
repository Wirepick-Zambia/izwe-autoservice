import { useEffect, useState } from 'react';
import { api, type ProcessingLog } from '../api/client';
import Modal from '../components/Modal';

export default function Logs() {
  const [logs, setLogs] = useState<ProcessingLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<ProcessingLog | null>(null);

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
                <th>Duration</th>
                <th>Found</th>
                <th>Sent</th>
                <th>Failed</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(log => (
                <tr key={log.id} className={`clickable-row${log.errorMessage ? ' row-error' : ''}`} onClick={() => setSelected(log)}>
                  <td>{log.id}</td>
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

      <Modal open={!!selected} onClose={() => setSelected(null)} title="Processing Run Details">
        {selected && (
          <div className="detail-grid">
            <div className="detail-row">
              <span className="detail-label">Run ID</span>
              <span className="detail-value">{selected.id}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Status</span>
              {selected.errorMessage
                ? <span className="badge badge-danger">Error</span>
                : selected.completedAt
                  ? <span className="badge badge-success">Complete</span>
                  : <span className="badge badge-warning">Running</span>}
            </div>
            <div className="detail-row">
              <span className="detail-label">Started</span>
              <span className="detail-value">{new Date(selected.startedAt).toLocaleString()}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Completed</span>
              <span className="detail-value">{selected.completedAt ? new Date(selected.completedAt).toLocaleString() : 'Still running'}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Duration</span>
              <span className="detail-value">{selected.completedAt
                ? `${Math.round((new Date(selected.completedAt).getTime() - new Date(selected.startedAt).getTime()) / 1000)} seconds`
                : '-'}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Records Found</span>
              <span className="detail-value">{selected.totalFound}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Sent</span>
              <span className="detail-value" style={{ color: '#10b981', fontWeight: 600 }}>{selected.totalSent}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Failed</span>
              <span className={`detail-value${selected.totalFailed > 0 ? ' text-danger' : ''}`}>{selected.totalFailed}</span>
            </div>
            {selected.errorMessage && (
              <div className="detail-row detail-full">
                <span className="detail-label">Error Message</span>
                <span className="detail-value text-danger">{selected.errorMessage}</span>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}
