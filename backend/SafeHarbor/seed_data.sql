-- SafeHarbor local data seed
-- Maps lighthouse_csv_v7 CSVs into the EF Core tables

-- ============================================================
-- 1. Seed ContributionType lookup
-- ============================================================
INSERT INTO "ContributionType" ("Id", "Code", "Name", "CreatedAt", "UpdatedAt", "CreatedBy")
VALUES
  (1, 'Monetary',    'Monetary',     NOW(), NOW(), 'import'),
  (2, 'InKind',      'In Kind',      NOW(), NOW(), 'import'),
  (3, 'Skills',      'Skills',       NOW(), NOW(), 'import'),
  (4, 'SocialMedia', 'Social Media', NOW(), NOW(), 'import'),
  (5, 'Time',        'Time',         NOW(), NOW(), 'import')
ON CONFLICT ("Id") DO NOTHING;

-- ============================================================
-- 2. Import Safehouses
-- ============================================================
CREATE TEMP TABLE tmp_safehouses (
  csv_id       INT,
  safehouse_code TEXT,
  name         TEXT,
  region       TEXT,
  city         TEXT,
  province     TEXT,
  country      TEXT,
  open_date    TEXT,
  status       TEXT,
  capacity_girls INT,
  capacity_staff INT,
  current_occupancy INT,
  notes        TEXT
);

\COPY tmp_safehouses FROM 'C:/Users/rclau/Downloads/lighthouse_csv_v7/lighthouse_csv_v7/safehouses.csv' CSV HEADER;

-- Add UUID column for mapping
ALTER TABLE tmp_safehouses ADD COLUMN uuid_id UUID DEFAULT gen_random_uuid();

INSERT INTO "Safehouses" ("Id", "Name", "Region", "CreatedAt", "UpdatedAt", "CreatedBy")
SELECT uuid_id,
       name,
       COALESCE(region, ''),
       NOW(), NOW(), 'import'
FROM tmp_safehouses
ON CONFLICT DO NOTHING;

-- ============================================================
-- 3. Import Donors from supporters.csv
-- ============================================================
CREATE TEMP TABLE tmp_supporters (
  csv_id           INT,
  supporter_type   TEXT,
  display_name     TEXT,
  organization_name TEXT,
  first_name       TEXT,
  last_name        TEXT,
  relationship_type TEXT,
  region           TEXT,
  country          TEXT,
  email            TEXT,
  phone            TEXT,
  status           TEXT,
  created_at_str   TEXT,
  first_donation_date TEXT,
  acquisition_channel TEXT
);

\COPY tmp_supporters FROM 'C:/Users/rclau/Downloads/lighthouse_csv_v7/lighthouse_csv_v7/supporters.csv' CSV HEADER;

ALTER TABLE tmp_supporters ADD COLUMN uuid_id UUID DEFAULT gen_random_uuid();

INSERT INTO "Donors" ("Id", "Name", "DisplayName", "Email", "LifetimeDonations",
                      "LastActivityAt", "CreatedAt", "CreatedAtUtc",
                      "UpdatedAt", "UpdatedAtUtc", "CreatedBy")
SELECT
  uuid_id,
  COALESCE(NULLIF(TRIM(first_name || ' ' || last_name), ' '), display_name, 'Unknown'),
  COALESCE(NULLIF(display_name, ''), first_name || ' ' || last_name, 'Unknown'),
  COALESCE(NULLIF(email, ''), 'noemail-' || csv_id || '@import.local'),
  0,
  NOW(), NOW(), NOW(), NOW(), NOW(), 'import'
FROM tmp_supporters
ON CONFLICT DO NOTHING;

-- ============================================================
-- 4. Import Contributions from donations.csv
-- ============================================================
CREATE TEMP TABLE tmp_donations (
  csv_id         INT,
  supporter_id   INT,
  donation_type  TEXT,
  donation_date  TEXT,
  is_recurring   TEXT,
  campaign_name  TEXT,
  channel_source TEXT,
  currency_code  TEXT,
  amount         TEXT,   -- TEXT to handle empty values
  estimated_value TEXT,
  impact_unit    TEXT,
  notes          TEXT,
  referral_post_id TEXT
);

\COPY tmp_donations FROM 'C:/Users/rclau/Downloads/lighthouse_csv_v7/lighthouse_csv_v7/donations.csv' CSV HEADER;

INSERT INTO "Contributions" ("Id", "DonorId", "ContributionTypeId", "StatusStateId",
                              "Amount", "Frequency", "ContributionDate",
                              "CreatedAt", "UpdatedAt", "CreatedBy")
SELECT
  gen_random_uuid(),
  ts.uuid_id,
  CASE td.donation_type
    WHEN 'Monetary'    THEN 1
    WHEN 'InKind'      THEN 2
    WHEN 'Skills'      THEN 3
    WHEN 'SocialMedia' THEN 4
    WHEN 'Time'        THEN 5
    ELSE 1
  END,
  1,  -- StatusState Active
  COALESCE(NULLIF(td.amount, '')::NUMERIC, 0),
  CASE td.is_recurring WHEN 'True' THEN 'Monthly' ELSE 'OneTime' END,
  (td.donation_date || 'T00:00:00Z')::TIMESTAMP WITH TIME ZONE,
  NOW(), NOW(), 'import'
FROM tmp_donations td
JOIN tmp_supporters ts ON ts.csv_id = td.supporter_id;

-- ============================================================
-- 5. Update LifetimeDonations totals per donor
-- ============================================================
UPDATE "Donors" d
SET "LifetimeDonations" = sub.total
FROM (
  SELECT "DonorId", SUM("Amount") AS total
  FROM "Contributions"
  GROUP BY "DonorId"
) sub
WHERE d."Id" = sub."DonorId";

-- ============================================================
-- 6. Import ContributionAllocations from donation_allocations.csv
-- ============================================================
CREATE TEMP TABLE tmp_allocations (
  csv_id         INT,
  donation_id    INT,
  safehouse_id   INT,
  program_area   TEXT,
  amount_allocated TEXT,
  allocation_date TEXT,
  allocation_notes TEXT
);

\COPY tmp_allocations FROM 'C:/Users/rclau/Downloads/lighthouse_csv_v7/lighthouse_csv_v7/donation_allocations.csv' CSV HEADER;

-- Map csv donation_id → Contributions UUID via row_number ordering
-- (donations were inserted in CSV order, so we match by rank)
CREATE TEMP TABLE tmp_contribution_uuids AS
SELECT "Id" AS contribution_uuid,
       ROW_NUMBER() OVER (ORDER BY "ContributionDate", ctid) AS rn
FROM "Contributions"
WHERE "CreatedBy" = 'import';

CREATE TEMP TABLE tmp_donation_rn AS
SELECT csv_id, ROW_NUMBER() OVER (ORDER BY csv_id) AS rn
FROM tmp_donations;

-- We need safehouse UUIDs too
CREATE TEMP TABLE tmp_safehouse_uuids AS
SELECT uuid_id AS safehouse_uuid, csv_id AS safehouse_csv_id
FROM tmp_safehouses;

INSERT INTO "ContributionAllocations" ("Id", "ContributionId", "SafehouseId", "Amount", "CreatedAt", "UpdatedAt", "CreatedBy")
SELECT
  gen_random_uuid(),
  cu.contribution_uuid,
  su.safehouse_uuid,
  COALESCE(NULLIF(a.amount_allocated, '')::NUMERIC, 0),
  NOW(), NOW(), 'import'
FROM tmp_allocations a
JOIN tmp_donation_rn dr ON dr.csv_id = a.donation_id
JOIN tmp_contribution_uuids cu ON cu.rn = dr.rn
JOIN tmp_safehouse_uuids su ON su.safehouse_csv_id = a.safehouse_id
ON CONFLICT DO NOTHING;

-- ============================================================
-- Summary
-- ============================================================
SELECT 'Safehouses'            AS "Table", COUNT(*) AS "Rows" FROM "Safehouses" WHERE "CreatedBy" = 'import'
UNION ALL
SELECT 'Donors',                           COUNT(*) FROM "Donors"        WHERE "CreatedBy" = 'import'
UNION ALL
SELECT 'Contributions',                    COUNT(*) FROM "Contributions"  WHERE "CreatedBy" = 'import'
UNION ALL
SELECT 'ContributionAllocations',          COUNT(*) FROM "ContributionAllocations" WHERE "CreatedBy" = 'import';
