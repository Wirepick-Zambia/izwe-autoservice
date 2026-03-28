import { useEffect, useState } from 'react';
import { api, type SmsRecord, type SmsPagedResult } from '../api/client';
import Modal from '../components/Modal';

export default function Messages() {
  const [data, setData] = useState<SmsPagedResult | null>(null);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<SmsRecord | null>(null);

  const load = () => {
    setLoading(true);
    api.getSmsRecords({
      page,
      pageSize: 25,
      status: status || undefined,
      search: search || undefined,
    }).then(setData).finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, [page, status]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    load();
  };

  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 0;

  return (
    <div className="page">
      <div className="page-header">
        <h2>Messages</h2>
      </div>

      <div className="filters card">
        <form onSubmit={handleSearch} className="filter-row">
          <select value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}>
            <option value="">All Statuses</option>
            <option value="0">Pending</option>
            <option value="1">Sent</option>
            <option value="2">Failed</option>
          </select>
          <input
            type="text"
            placeholder="Search by phone or contract..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
          <button type="submit" className="btn btn-primary">Search</button>
        </form>
      </div>

      <div className="card">
        {loading ? <div className="loading">Loading...</div> : !data || data.items.length === 0 ? (
          <p className="muted">No messages found</p>
        ) : (
          <>
            <div className="table-info">
              Showing {(data.page - 1) * data.pageSize + 1}-{Math.min(data.page * data.pageSize, data.totalCount)} of {data.totalCount}
            </div>
            <table>
              <thead>
                <tr>
                  <th>Contract</th>
                  <th>Phone</th>
                  <th>Country</th>
                  <th>Message</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th>Processed</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map(sms => (
                  <tr key={sms.id} className="clickable-row" onClick={() => setSelected(sms)}>
                    <td>{sms.contractId}</td>
                    <td>{sms.phoneNumber}</td>
                    <td>{sms.country}</td>
                    <td className="msg-cell" title={sms.messageContent}>
                      {sms.messageContent.length > 50
                        ? sms.messageContent.substring(0, 50) + '...'
                        : sms.messageContent}
                    </td>
                    <td>
                      <span className={`badge badge-${sms.status.toLowerCase() === 'sent' ? 'success' : sms.status.toLowerCase() === 'failed' ? 'danger' : 'warning'}`}>
                        {sms.status}
                      </span>
                    </td>
                    <td>{new Date(sms.createdAt).toLocaleString()}</td>
                    <td>{sms.processedAt ? new Date(sms.processedAt).toLocaleString() : '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="pagination">
              <button disabled={page <= 1} onClick={() => setPage(p => p - 1)} className="btn btn-secondary">Previous</button>
              <span>Page {page} of {totalPages}</span>
              <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)} className="btn btn-secondary">Next</button>
            </div>
          </>
        )}
      </div>

      <Modal open={!!selected} onClose={() => setSelected(null)} title="Message Details">
        {selected && (
          <div className="detail-grid">
            <DetailRow label="ID" value={String(selected.id)} />
            <DetailRow label="Contract ID" value={selected.contractId} />
            <DetailRow label="Phone Number" value={selected.phoneNumber} />
            <DetailRow label="Country" value={selected.country} />
            <DetailRow label="Status" value={selected.status} badge={
              selected.status.toLowerCase() === 'sent' ? 'success' : selected.status.toLowerCase() === 'failed' ? 'danger' : 'warning'
            } />
            {selected.sourceFile && <DetailRow label="Source File" value={selected.sourceFile} />}
            <DetailRow label="Message" value={selected.messageContent} full />
            {selected.apiMessageId && <DetailRow label="Gateway Message ID" value={selected.apiMessageId} />}
            {selected.apiStatus && <DetailRow label="Gateway Status" value={selected.apiStatus} full />}
            {selected.apiCost != null && <DetailRow label="Cost" value={`${selected.apiCost} USD`} />}
            {selected.errorMessage && (
              <DetailRow label="Failure Reason" value={selected.errorMessage} full error />
            )}
            {selected.rawResponse && (
              <div className="detail-row detail-full">
                <span className="detail-label">Raw Gateway Response</span>
                <pre className="raw-response">{selected.rawResponse}</pre>
              </div>
            )}
            <DetailRow label="Created" value={new Date(selected.createdAt).toLocaleString()} />
            <DetailRow label="Processed" value={selected.processedAt ? new Date(selected.processedAt).toLocaleString() : 'Not yet'} />
          </div>
        )}
      </Modal>
    </div>
  );
}

function DetailRow({ label, value, badge, full, error }: {
  label: string;
  value: string;
  badge?: string;
  full?: boolean;
  error?: boolean;
}) {
  return (
    <div className={`detail-row${full ? ' detail-full' : ''}`}>
      <span className="detail-label">{label}</span>
      {badge ? (
        <span className={`badge badge-${badge}`}>{value}</span>
      ) : (
        <span className={`detail-value${error ? ' text-danger' : ''}`}>{value}</span>
      )}
    </div>
  );
}
