export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type PagingQuery = {
  page: number
  pageSize: number
  search?: string
  desc?: boolean
  safehouseId?: string
  statusStateId?: number
  categoryId?: number
  residentCaseId?: string
}

export type DonorListItem = {
  id: string
  name: string
  email: string
  lastActivityAt: string
  lifetimeContributions: number
}

export type ResidentCaseListItem = {
  id: string
  safehouseId: string
  safehouse: string
  caseCategoryId: number
  category: string
  statusStateId: number
  status: string
  socialWorkerExternalId: string | null
  residentName: string | null
  openedAt: string
  closedAt: string | null
}

export type ProcessRecordItem = {
  id: string
  residentCaseId: string
  recordedAt: string
  socialWorker: string
  sessionType: string
  sessionDurationMinutes: number | null
  emotionalStateObserved: string
  emotionalStateEnd: string | null
  summary: string
  interventionsApplied: string | null
  followUpActions: string | null
  progressNoted: boolean
  concernsFlagged: boolean
  referralMade: boolean
  notesRestricted: boolean
}

export type HomeVisitItem = {
  id: string
  residentCaseId: string
  visitDate: string
  visitType: string
  status: string
  notes: string
}

export type CaseConferenceItem = {
  id: string
  residentCaseId: string
  conferenceDate: string
  status: string
  outcomeSummary: string
}

export type CaseloadLookupItem = { id: number; name: string }
export type CaseloadSafehouseItem = { id: string; name: string }
export type CaseloadLookupsResponse = {
  safehouses: CaseloadSafehouseItem[]
  caseCategories: CaseloadLookupItem[]
  statusStates: CaseloadLookupItem[]
}

export type ApiErrorEnvelope = {
  errorCode: string
  message: string
  traceId: string
}

export type DashboardSummaryResponse = {
  activeResidents: number
  recentContributions: ContributionListItem[]
  upcomingConferences: ConferenceListItem[]
  summaryOutcomes: OutcomeSummaryItem[]
}

export type ContributionListItem = {
  id: string
  donorName: string
  amount: number
  contributionDate: string
  status: string
}

export type ConferenceListItem = {
  id: string
  residentCaseId: string
  conferenceDate: string
  status: string
  outcomeSummary: string
}

export type OutcomeSummaryItem = {
  snapshotDate: string
  totalResidentsServed: number
  totalHomeVisits: number
  totalContributions: number
}

export type NotImplementedEnvelope = {
  errorCode: string
  message: string
  traceId: string
  apiVersion?: string
}
