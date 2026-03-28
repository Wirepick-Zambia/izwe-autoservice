import { useEffect, useState } from 'react';
import { api } from '../api/client';

interface SettingsGroup {
  category: string;
  label: string;
  fields: { key: string; label: string; type: 'text' | 'password' | 'number' | 'toggle' }[];
}

const GROUPS: SettingsGroup[] = [
  {
    category: 'General',
    label: 'General',
    fields: [
      { key: 'BaseFolderPath', label: 'Base Folder Path', type: 'text' },
      { key: 'CronIntervalMinutes', label: 'Processing Interval (minutes)', type: 'number' },
      { key: 'SmsBatchSize', label: 'SMS Batch Size (per cycle)', type: 'number' },
    ],
  },
  {
    category: 'Sms',
    label: 'SMS Gateway',
    fields: [
      { key: 'ApiUrl', label: 'API URL', type: 'text' },
      { key: 'ClientId', label: 'Client ID', type: 'text' },
      { key: 'SenderId', label: 'Sender ID', type: 'text' },
      { key: 'ApiPassword', label: 'API Password', type: 'password' },
    ],
  },
  {
    category: 'Smtp',
    label: 'SMTP Configuration',
    fields: [
      { key: 'SmtpHost', label: 'SMTP Host', type: 'text' },
      { key: 'SmtpPort', label: 'SMTP Port', type: 'number' },
      { key: 'SmtpUsername', label: 'Username', type: 'text' },
      { key: 'SmtpPassword', label: 'Password', type: 'password' },
      { key: 'SmtpUseSsl', label: 'Use SSL', type: 'toggle' },
    ],
  },
  {
    category: 'Alerts',
    label: 'Email Alerts',
    fields: [
      { key: 'AlertsEnabled', label: 'Enable Alerts', type: 'toggle' },
      { key: 'AlertEmailFrom', label: 'From Email', type: 'text' },
      { key: 'AlertEmailTo', label: 'To Email(s) (semicolon-separated)', type: 'text' },
    ],
  },
];

export default function Settings() {
  const [values, setValues] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState<string | null>(null);
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getSettings().then(data => setValues(data.settings)).finally(() => setLoading(false));
  }, []);

  const handleChange = (key: string, value: string) => {
    setValues(prev => ({ ...prev, [key]: value }));
  };

  const saveCategory = async (group: SettingsGroup) => {
    setSaving(group.category);
    setMessage('');
    try {
      const settings: Record<string, string> = {};
      for (const field of group.fields) {
        settings[field.key] = values[field.key] ?? '';
      }
      await api.updateSettings(group.category, settings);
      setMessage(`${group.label} settings saved.`);
    } catch {
      setMessage(`Failed to save ${group.label} settings.`);
    } finally {
      setSaving(null);
    }
  };

  if (loading) return <div className="loading">Loading...</div>;

  return (
    <div className="page">
      <div className="page-header">
        <h2>Settings</h2>
      </div>

      {message && <div className="alert">{message}</div>}

      {GROUPS.map(group => (
        <div key={group.category} className="card settings-card">
          <h3>{group.label}</h3>
          <div className="settings-fields">
            {group.fields.map(field => (
              <div key={field.key} className="field">
                <label>{field.label}</label>
                {field.type === 'toggle' ? (
                  <label className="toggle">
                    <input
                      type="checkbox"
                      checked={values[field.key] === 'true'}
                      onChange={e => handleChange(field.key, e.target.checked ? 'true' : 'false')}
                    />
                    <span className="toggle-slider"></span>
                  </label>
                ) : (
                  <input
                    type={field.type}
                    value={values[field.key] ?? ''}
                    onChange={e => handleChange(field.key, e.target.value)}
                  />
                )}
              </div>
            ))}
          </div>
          <button
            onClick={() => saveCategory(group)}
            disabled={saving === group.category}
            className="btn btn-primary"
          >
            {saving === group.category ? 'Saving...' : 'Save'}
          </button>
        </div>
      ))}
    </div>
  );
}
