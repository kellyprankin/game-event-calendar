<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { api, ApiError, formatDate, formatTime, type GameEvent } from '@/api'

const props = defineProps<{ id: string }>()

const event = ref<GameEvent>()
const error = ref('')

onMounted(async () => {
  try {
    event.value = await api.event(props.id)
  } catch (e) {
    error.value = e instanceof ApiError && e.status === 404 ? 'Event not found.' : 'Could not load this event.'
  }
})
</script>

<template>
  <p v-if="error" class="error" role="alert">{{ error }}</p>
  <p v-else-if="!event" class="muted">Loading…</p>

  <template v-else>
    <h1>{{ event.name }}</h1>

    <div class="layout">
      <dl class="card details">
        <dt>Game</dt>
        <dd>{{ event.gameName }} · {{ event.format }}</dd>
        <dt>When</dt>
        <dd>{{ formatDate(event.startsAt) }}, {{ formatTime(event.startsAt) }}–{{ formatTime(event.endsAt) }}</dd>
        <dt>Seats</dt>
        <dd :class="{ error: event.seatsLeft === 0 }">
          {{ event.seatsLeft === 0 ? 'Full' : `${event.seatsLeft} of ${event.capacity} left` }}
          <span class="muted">({{ event.registeredCount }} registered)</span>
        </dd>
        <dt>Minimum to run</dt>
        <dd>{{ event.minPlayers }} players</dd>
      </dl>

      <div class="card qr">
        <img
          :src="api.qrUrl(event.id)"
          :alt="`QR code linking to registration for ${event.name}`"
          width="200"
          height="200"
        />
        <p>Scan to register, or share this link:</p>
        <a :href="event.registrationUrl" class="link">{{ event.registrationUrl }}</a>
      </div>
    </div>

    <p class="actions">
      <RouterLink :to="`/events/${event.id}/register`" class="button">Register</RouterLink>
      <a :href="api.inviteUrl(event.id)" download>Add to calendar (.ics)</a>
    </p>
  </template>
</template>

<style scoped>
.layout {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr));
  align-items: start;
}
.details { margin: 0; display: grid; grid-template-columns: max-content 1fr; gap: 0.4rem 1rem; }
dt { font-weight: 600; }
dd { margin: 0; }
.qr { text-align: center; }
.qr img { display: block; margin: 0 auto; background: #fff; border-radius: 4px; }
.qr p { margin: 0.75rem 0 0.25rem; }
.link { word-break: break-all; font-size: 0.875rem; }
.actions { display: flex; flex-wrap: wrap; gap: 1rem; align-items: center; margin-top: 1.25rem; }
</style>
