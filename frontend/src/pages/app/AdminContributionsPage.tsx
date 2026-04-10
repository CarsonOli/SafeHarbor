import { DonorsContributionsPage } from './DonorsContributionsPage'

/**
 * Keep the legacy admin contributions route component name as a thin alias.
 *
 * Why: `main.tsx` and existing route links still reference `AdminContributionsPage`.
 * Re-exporting the current implementation prevents route/import breakage without
 * changing the current page behavior or duplicating contribution UI logic.
 */
export default function AdminContributionsPage() {
  return <DonorsContributionsPage />
}
