<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { api, formatDate, formatTime, type GameEvent } from '@/api'

const events = ref<GameEvent[]>([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  const startOfToday = new Date()
  startOfToday.setHours(0, 0, 0, 0)
  try {
    events.value = await api.events(startOfToday)
  } catch {
    error.value = 'Could not load events. Is the API running?'
  } finally {
    loading.value = false
  }
})

// Agenda view: events grouped by the viewer's local calendar day (API returns them sorted).
const days = computed(() => {
  const groups = new Map<string, GameEvent[]>()
  for (const ev of events.value) {
    const key = new Date(ev.startsAt).toDateString()
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key)!.push(ev)
  }
  return [...groups.entries()].map(([key, list]) => ({ key, label: formatDate(list[0]!.startsAt), events: list }))
})
</script>

<template>
  <div class="title-row">
    <h1>Upcoming events</h1>
    <RouterLink to="/events/new" class="button">Create event</RouterLink>
  </div>

  <p v-if="loading" class="muted">Loading…</p>
  <p v-else-if="error" class="error" role="alert">{{ error }}</p>
  <p v-else-if="days.length === 0" class="muted">
    Nothing scheduled yet. <RouterLink to="/events/new">Create the first event</RouterLink>.
  </p>

  <section v-for="day in days" :key="day.key" class="day">
    <h2>{{ day.label }}</h2>
    <ul>
      <li v-for="ev in day.events" :key="ev.id" class="card">
        <span class="time">{{ formatTime(ev.startsAt) }}–{{ formatTime(ev.endsAt) }}</span>
        <RouterLink :to="`/events/${ev.id}`" class="name">{{ ev.name }}</RouterLink>
        <span class="muted">{{ ev.gameName }} · {{ ev.format }}</span>
        <span :class="ev.seatsLeft === 0 ? 'error' : 'muted'">
          {{ ev.seatsLeft === 0 ? 'Full' : `${ev.seatsLeft} of ${ev.capacity} seats left` }}
        </span>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.title-row {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
}
.day h2 {
  font-size: 1.05rem;
  margin: 1.5rem 0 0.5rem;
  padding-bottom: 0.25rem;
  border-bottom: 1px solid var(--border);
}
ul { list-style: none; padding: 0; margin: 0; display: grid; gap: 0.5rem; }
li { display: grid; gap: 0.15rem; }
.time { font-size: 0.875rem; font-weight: 600; }
.name { font-size: 1.1rem; font-weight: 600; }
</style>
