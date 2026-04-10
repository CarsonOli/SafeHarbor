import { buildAuthHeaders } from './authHeaders';
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors';
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

async function ensureSuccess(response: Response): Promise<Response> {
  if (response.ok) {
    return response;
  }

  if (response.status === 403) {
    // Keep Admin-only policy failures explicit for the management console.
    throw new HttpError(403, NOT_AUTHORIZED_MESSAGE);
  }

  throw new HttpError(response.status, `Request failed with status ${response.status}`);
}

export const adminApi = {
  getContributions: async () => {
    const response = await fetch(`${API_BASE}/api/admin/contributions`, {
      headers: buildAuthHeaders(),
    });
    await ensureSuccess(response);
    return response.json();
  }, // <--- THIS COMMA IS CRITICAL

  updateContribution: async (id: string, data: Record<string, unknown>) => {
    const response = await fetch(`${API_BASE}/api/admin/contributions/${id}`, {
      method: 'PUT',
      headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(data),
    });
    return ensureSuccess(response);
  }, // <--- AND THIS ONE (if you add more functions later)

  deleteContribution: async (id: string) => {
    const response = await fetch(`${API_BASE}/api/admin/contributions/${id}`, {
      method: 'DELETE',
      headers: buildAuthHeaders(),
    });
    return ensureSuccess(response);
  }
};
