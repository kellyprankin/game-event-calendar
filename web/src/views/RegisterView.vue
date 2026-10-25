<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { api, ApiError, formatDate, formatTime, type GameEvent } from '@/api'

const props = defineProps<{ id: string }>()

const event = ref<GameEvent>()
const loadError = ref('')
const name = ref('')
const submitting = ref(false)
const message = ref<{ kind: 'success' | 'error'; text: string }>()

onMounted(load)

async function load() {
  try {
    event.value = await api.event(props.id)
  } catch (e) {
    loadError.value = e instanceof ApiError && e.status === 404 ? 'Event not found.' : 'Could not load this event.'
  }
}

async function submit() {
  submitting.value = true
  message.value = undefined
  try {
    const registration = await api.register(props.id, name.value)
    message.value = { kind: 'success', text: `You're registered, ${registration.playerName}! See you there.` }
    name.value = ''
  } catch (e) {
    message.value = { kind: 'error', text: errorText(e) }
  } finally {
    submitting.value = false
    await load() // refresh seats left
  }
}

// The server is the source of truth for capacity; these map its 409 codes to friendly text.
function errorText(e: unknown) {
  if (!(e instanceof ApiError)) return 'Something went wrong. Please try again.'
  switch (e.code) {
    case 'event_full':
      return 'Sorry, this event is full.'
    case 'already_registered':
      return 'Someone with that name is already registered for this event.'
  }
  if (e.status === 400) return 'Please enter your name.'
  if (e.status === 404) return 'This event no longer exists.'
  return e.message
}
</script>

<template>
  <p v-if="loadError" class="error" role="alert">{{ loadError }}</p>
  <p v-else-if="!event" class="muted">Loading…</p>

  <template v-else>
    <h1>Register for {{ event.name }}</h1>
    <p class="muted">
      {{ event.gameName }} · {{ event.format }}<br />
      {{ formatDate(event.startsAt) }}, {{ formatTime(event.startsAt) }}
    </p>
    <p :class="{ error: event.seatsLeft === 0 }">
      {{ event.seatsLeft === 0 ? 'This event is full.' : `${event.seatsLeft} of ${event.capacity} seats left.` }}
    </p>

    <form v-if="event.seatsLeft > 0 || message" novalidate @submit.prevent="submit">
      <label>
        Your name
        <input v-model="name" required maxlength="100" autocomplete="name" />
      </label>
      <div>
        <button type="submit" :disabled="submitting || !name.trim()">
          {{ submitting ? 'Registering…' : 'Register' }}
        </button>
      </div>
    </form>

    <!-- Always rendered so screen readers announce changes to it. -->
    <p aria-live="polite" :class="message?.kind">{{ message?.text }}</p>

    <p><RouterLink :to="`/events/${event.id}`">Back to event</RouterLink></p>
  </template>
</template>
