import { buildAuthHeaders } from './authHeaders'
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export interface PostScoreRequest {
  platform:              string
  postType:              string
  mediaType:             string
  contentTopic:          string
  sentimentTone:         string
  featuresResidentStory: boolean
  hasCallToAction:       boolean
  callToActionType:      string
  isBoosted:             boolean
  boostBudgetPhp:        number
  postHour:              number
  dayOfWeek:             string
  numHashtags:           number
  captionLength:         number
  mentionsCount:         number
}

export interface PostScoreResponse {
  conversionLikelihood: 'High' | 'Medium' | 'Low'
  probability:          number
  recommendations:      string[]
}

export async function scorePost(request: PostScoreRequest): Promise<PostScoreResponse> {
  const endpoint = '/api/admin/social-media/score-post'
  const res = await fetch(`${API_BASE}${endpoint}`, {
    method:  'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
    body:    JSON.stringify(request),
  })
  if (!res.ok) {
    if (res.status === 403) throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { endpoint, method: 'POST' })
    throw new HttpError(res.status, `Request failed with status ${res.status}`, { endpoint, method: 'POST' })
  }
  return res.json() as Promise<PostScoreResponse>
}
