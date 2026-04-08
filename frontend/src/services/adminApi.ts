import { buildAuthHeaders } from './authHeaders';
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

export const adminApi = {
  getContributions: async () => {
    const response = await fetch(`${API_BASE}/api/admin/contributions`, {
      headers: buildAuthHeaders(),
    });
    return response.json();
  }, // <--- THIS COMMA IS CRITICAL

  updateContribution: async (id: string, data: any) => {
    return fetch(`${API_BASE}/api/admin/contributions/${id}`, {
      method: 'PUT',
      headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(data),
    });
  }, // <--- AND THIS ONE (if you add more functions later)

  deleteContribution: async (id: string) => {
    return fetch(`${API_BASE}/api/admin/contributions/${id}`, {
      method: 'DELETE',
      headers: buildAuthHeaders(),
    });
  }
};