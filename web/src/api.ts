// Types mirror the API's DTOs (GameEventApi/Contracts).

export interface GameTemplate {
  id: string
  name: string
  formats: string[]
  defaultDurationMinutes: number
  defaultCapacity: number
  maxCapacity: number
  minPlayers: number
}

export interface GameEvent {
  id: string
  name: string
  gameTemplateId: string
  gameName: string
  format: string
  startsAt: string
  endsAt: string
  durationMinutes: number
  capacity: number
  registeredCount: number
  seatsLeft: number
  minPlayers: number
  registrationUrl?: string
}

export interface CreateEventRequest {
  name: string
  gameTemplateId: string
  format: string
  startsAt: string
  capacity: number | null
}

export interface Registration {
  id: string
  eventId: string
  playerName: string
  createdAt: string
}

/** Error carrying the API's ProblemDetails fields. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly code?: string,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message)
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  if (response.ok) {
    return (await response.json()) as T
  }

  const problem = await response.json().catch(() => ({}))
  const message = problem.detail ?? problem.title ?? `Request failed (${response.status})`
  throw new ApiError(response.status, message, problem.code, problem.errors)
}

export const api = {
  templates: () => request<GameTemplate[]>('/templates'),
  events: (from?: Date) =>
    request<GameEvent[]>(from ? `/events?from=${encodeURIComponent(from.toISOString())}` : '/events'),
  event: (id: string) => request<GameEvent>(`/events/${id}`),
  createEvent: (body: CreateEventRequest) =>
    request<GameEvent>('/events', { method: 'POST', body: JSON.stringify(body) }),
  register: (eventId: string, name: string) =>
    request<Registration>(`/events/${eventId}/registrations`, {
      method: 'POST',
      body: JSON.stringify({ name }),
    }),
  inviteUrl: (id: string) => `/api/events/${id}/invite.ics`,
  qrUrl: (id: string) => `/api/events/${id}/qr.png`,
}

const dateFormat = new Intl.DateTimeFormat(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })
const timeFormat = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' })

export const formatDate = (iso: string) => dateFormat.format(new Date(iso))
export const formatTime = (iso: string) => timeFormat.format(new Date(iso))
