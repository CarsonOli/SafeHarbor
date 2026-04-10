export type DonationFilters = {
  fromDate?: string
  toDate?: string
  donationType?: string
  campaign?: string
  channelSource?: string
  supporterType?: string
  frequency?: string
  page?: number
  pageSize?: number
}

export type InKindDonationItem = {
  itemId: number
  itemName: string | null
  itemCategory: string | null
  quantity: number
  unitOfMeasure: string | null
  estimatedUnitValue: number
}

export type DonationListItem = {
  donationId: number
  donationDate: string | null
  donationType: string
  amount: number
  estimatedValue: number
  campaignName: string | null
  channelSource: string | null
  frequency: string | null
  notes: string | null
  supporterId: number
  donorDisplayName: string
  supporterType: string | null
  supporterEmail: string | null
  inKindItems: InKindDonationItem[]
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type YourDonationsResponse = {
  hasLinkedSupporter: boolean
  supporterId: number | null
  supporterDisplayName: string | null
  donations: DonationListItem[]
}
