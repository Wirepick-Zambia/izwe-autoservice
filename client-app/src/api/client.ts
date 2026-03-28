const BASE = '/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const headers: Record<string, string> = {};
  if (options?.body) {
    headers['Content-Type'] = 'application/json';
  }

  const res = await fetch(`${BASE}${url}`, {
    ...options,
    headers: { ...headers, ...options?.headers as Record<string, string> },
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return res.json();
}

export interface DashboardData {
  totalPending: number;
  totalSent: number;
  totalFailed: number;
  todayCount: number;
  countries: string[];
  recentLogs: ProcessingLog[];
}

export interface ProcessingLog {
  id: number;
  startedAt: string;
  completedAt: string | null;
  totalFound: number;
  totalSent: number;
  totalFailed: number;
  errorMessage: string | null;
}

export interface SmsRecord {
  id: number;
  contractId: string;
  phoneNumber: string;
  messageContent: string;
  country: string;
  status: string;
  apiMessageId: string | null;
  apiStatus: string | null;
  errorMessage: string | null;
  createdAt: string;
  processedAt: string | null;
}

export interface SmsPagedResult {
  items: SmsRecord[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SettingsData {
  settings: Record<string, string>;
}

export const api = {
  getDashboard: () => request<DashboardData>('/dashboard'),

  getSmsRecords: (params: {
    page?: number;
    pageSize?: number;
    status?: string;
    country?: string;
    search?: string;
  }) => {
    const query = new URLSearchParams();
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    if (params.status) query.set('status', params.status);
    if (params.country) query.set('country', params.country);
    if (params.search) query.set('search', params.search);
    return request<SmsPagedResult>(`/sms?${query}`);
  },

  triggerProcessing: () => request<{ message: string }>('/sms/process', { method: 'POST' }),

  getSettings: () => request<SettingsData>('/settings'),

  getSettingsByCategory: (category: string) =>
    request<SettingsData>(`/settings/${category}`),

  updateSettings: (category: string, settings: Record<string, string>) =>
    request<{ message: string }>('/settings', {
      method: 'PUT',
      body: JSON.stringify({ category, settings }),
    }),

  getLogs: (count = 20) => request<ProcessingLog[]>(`/logs?count=${count}`),
};
