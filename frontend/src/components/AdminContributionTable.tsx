import { useEffect, useState } from 'react';
import { adminApi } from '../services/adminApi';

const AdminContributionTable = () => {
  const [contributions, setContributions] = useState<Record<string, unknown>[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingItem, setEditingItem] = useState<Record<string, unknown> | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const data = await adminApi.getContributions();
      setContributions(data);
    } catch (error) {
      console.error("Failed to load contributions", error);
    } finally {
      setLoading(false);
    }
  };

  const saveEdit = async () => {
    if (!editingItem) return;
    try {
      // Ensure amount is a number before sending to .NET
      const updatedData = {
        ...editingItem,
        amount: parseFloat(editingItem['amount'] as string)
      };

      await adminApi.updateContribution(editingItem['id'] as string, updatedData);
      setEditingItem(null);
      await loadData(); // Await the reload for a smoother UI
      alert("Contribution updated successfully!");
    } catch {
      alert("Update failed. Check if the backend is running.");
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm("Are you sure? This will permanently remove this record from SafeHarbor logs.")) {
      try {
        await adminApi.deleteContribution(id);
        setContributions(prev => prev.filter((c) => c['id'] !== id));
      } catch {
        alert("Error deleting record.");
      }
    }
  };

  if (loading) return <p>Loading contributions...</p>;

  return (
    <div className="admin-table-container">
      <h2>Global Contributions Management</h2>
      
      {editingItem && (
        <div style={{ border: '2px solid #007bff', padding: '20px', marginBottom: '20px', borderRadius: '8px', background: '#fff' }}>
          <h3>Edit Contribution</h3>
          <div style={{ marginBottom: '10px' }}>
            <label>Amount ($): </label>
            <input 
              type="number" 
              step="0.01" // Allows for cents
              value={editingItem['amount'] as string}
              onChange={(e) => setEditingItem({...editingItem, amount: e.target.value})}
            />
          </div>
          <div style={{ marginBottom: '10px' }}>
            <label>Frequency: </label>
            <select 
              value={editingItem['frequency'] as string}
              onChange={(e) => setEditingItem({...editingItem, frequency: e.target.value})}
            >
              <option value="One-time">One-time</option>
              <option value="Monthly">Monthly</option>
            </select>
          </div>
          <button onClick={saveEdit} className="button" style={{ marginRight: '10px' }}>Save Changes</button>
          <button onClick={() => setEditingItem(null)} className="button button-secondary">Cancel</button>
        </div>
      )}

      <table className="admin-table" style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ textAlign: 'left', borderBottom: '1px solid #ddd' }}>
            <th>Donor Email</th>
            <th>Amount</th>
            <th>Frequency</th>
            <th>Date</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {contributions.map((c) => (
            <tr key={c['id'] as string} style={{ borderBottom: '1px solid #eee' }}>
              <td>{(c['donorEmail'] || c['donor_email'] || 'Unknown') as string}</td>
              <td>${Number(c['amount']).toFixed(2)}</td>
              <td>{c['frequency'] as string}</td>
              <td>{new Date(c['contributionDate'] as string).toLocaleDateString()}</td>
              <td style={{ padding: '10px 0' }}>
                <button onClick={() => setEditingItem(c)} style={{ marginRight: '8px' }}>
                  Edit
                </button>
                <button onClick={() => handleDelete(c['id'] as string)} className="delete-btn" style={{ color: 'red' }}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default AdminContributionTable;