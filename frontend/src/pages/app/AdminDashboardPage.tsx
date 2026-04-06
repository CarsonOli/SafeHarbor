export function AdminDashboardPage() {
  return (
    <section>
      <h1>Admin Dashboard</h1>
      <p className="lead">Snapshot of active residents, contributions, conferences, and outcomes.</p>
      <div className="feature-grid">
        <article className="feature-card"><h2>Active residents</h2><p>Track currently active resident cases by safehouse.</p></article>
        <article className="feature-card"><h2>Recent contributions</h2><p>Review the latest donor contributions and status.</p></article>
        <article className="feature-card"><h2>Upcoming conferences</h2><p>View the next case conference schedule at a glance.</p></article>
        <article className="feature-card"><h2>Summary outcomes</h2><p>Monitor service outcomes from recent snapshots.</p></article>
      </div>
    </section>
  )
}
