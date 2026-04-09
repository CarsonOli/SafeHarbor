import AdminContributionTable from '../../components/AdminContributionTable';

const AdminContributionsPage = () => {
  return (
    <div className="page-container">
      <header className="page-header">
        <h1>Management Console</h1>
        <p>Review and manage all SafeHarbor contributions.</p>
      </header>
      
      <main>
        <AdminContributionTable />
      </main>
    </div>
  );
};

export default AdminContributionsPage;