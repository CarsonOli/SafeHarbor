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
  let res: Response

  try {
    res = await fetch(`${API_BASE}${endpoint}`, {
      method:  'POST',
      headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
      body:    JSON.stringify(request),
    })
  } catch (error) {
    // NOTE: Fetch throws before an HTTP response exists when the browser cannot reach the API at all,
    // so we raise a more actionable message than the default "Failed to fetch".
    if (error instanceof TypeError) {
      throw new Error(
        'Unable to reach the social media scoring API. Start the backend and ML service, or set VITE_API_BASE_URL to the API host.'
      )
    }

    throw error
  }

  if (!res.ok) {
    if (res.status === 403) throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { endpoint, method: 'POST' })
    throw new HttpError(res.status, `Request failed with status ${res.status}`, { endpoint, method: 'POST' })
  }
  return res.json() as Promise<PostScoreResponse>
}
