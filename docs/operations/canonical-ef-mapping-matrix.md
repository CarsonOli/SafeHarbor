# Canonical EF Mapping Matrix (lighthouse schema)

## Scope

This matrix documents schema/table/column alignment for the logged-in operational entities after standardizing on the **`lighthouse`** schema strategy.

- Canonical schema: `lighthouse`
- Canonical naming: `snake_case` tables and columns
- Auth table remains `lighthouse.users` and now shares the same schema strategy as non-auth entities.

## Deployed DB comparison queries

Use these exact queries against the deployed PostgreSQL database to verify runtime reality:

```sql
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema IN ('public', 'lighthouse')
ORDER BY table_schema, table_name;
```

```sql
SELECT table_schema, table_name, column_name
FROM information_schema.columns
WHERE table_schema IN ('public', 'lighthouse')
  AND table_name IN (
    'donors',
    'contributions',
    'resident_cases',
    'process_recordings',
    'home_visits',
    'case_conferences',
    'social_post_metrics',
    'campaigns',
    'contribution_allocations',
    'resident_assessments',
    'intervention_plans',
    'safehouses',
    'residents'
  )
ORDER BY table_schema, table_name, ordinal_position;
```

## Mapping matrix

| Entity | Canonical table | Key canonical columns | Legacy table/column patterns handled by migration |
|---|---|---|---|
| `Donor` | `lighthouse.donors` | `id`, `display_name`, `lifetime_donations`, `last_activity_at` | `public."Donors"`, `"DisplayName"`, `"LifetimeDonations"`, `"LastActivityAt"` |
| `Contribution` | `lighthouse.contributions` | `id`, `donor_id`, `campaign_id`, `contribution_type_id`, `status_state_id`, `contribution_date` | `public."Contributions"`, `"DonorId"`, `"CampaignId"`, `"ContributionTypeId"`, `"StatusStateId"`, `"ContributionDate"` |
| `ResidentCase` | `lighthouse.resident_cases` | `id`, `safehouse_id`, `resident_id`, `case_category_id`, `case_subcategory_id`, `status_state_id`, `opened_at`, `closed_at` | `public."ResidentCases"`, PascalCase column names |
| `ProcessRecording` | `lighthouse.process_recordings` | `id`, `resident_case_id`, `recorded_at`, `summary` | `public."ProcessRecordings"`, `"ResidentCaseId"`, `"RecordedAt"` |
| `HomeVisit` | `lighthouse.home_visits` | `id`, `resident_case_id`, `visit_type_id`, `status_state_id`, `visit_date`, `notes` | `public."HomeVisits"`, PascalCase FK/date columns |
| `CaseConference` | `lighthouse.case_conferences` | `id`, `resident_case_id`, `conference_date`, `status_state_id`, `outcome_summary` | `public."CaseConferences"`, PascalCase FK/date columns |
| `SocialPostMetric` | `lighthouse.social_post_metrics` | `id`, `campaign_id`, `posted_at`, `content_type`, `attributed_donation_amount`, `attributed_donation_count` | `public."SocialPostMetrics"`, PascalCase campaign/content/attribution columns |
| `Campaign` | `lighthouse.campaigns` | `id`, `name`, `start_date`, `end_date`, `status_state_id`, `goal_amount` | `public."Campaigns"` |
| `ContributionAllocation` | `lighthouse.contribution_allocations` | `id`, `contribution_id`, `safehouse_id`, `amount_allocated` | `public."ContributionAllocations"` |
| `ResidentAssessment` | `lighthouse.resident_assessments` | `id`, `resident_case_id`, `assessed_at`, `status_state_id`, `notes` | `public."ResidentAssessments"` |
| `InterventionPlan` | `lighthouse.intervention_plans` | `id`, `resident_case_id`, `effective_from`, `effective_to`, `status_state_id`, `plan_details` | `public."InterventionPlans"` |
| `Safehouse` | `lighthouse.safehouses` | `id`, `name`, `region` | `public."Safehouses"` |
| `Resident` | `lighthouse.residents` | `id`, `full_name`, `date_of_birth`, `medical_notes`, `case_worker_email` | `public."Residents"` |

## Notes

- The migration is deliberately defensive (schema/table/column existence checks) to support mixed historical deployments.
- This matrix is aligned with `SafeHarborDbContext` + migration intent and should be revalidated by running the information schema queries above in each deployed environment.
