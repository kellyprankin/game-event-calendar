<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api, ApiError, type GameTemplate } from '@/api'

const router = useRouter()

const templates = ref<GameTemplate[]>([])
const name = ref('')
const templateId = ref('')
const format = ref('')
const startsAt = ref(toLocalInput(tomorrowAt(18)))
// Greys out past days in the picker. Midnight rather than "now": the picker disables a day when
// that day at the currently entered time is before min, which would block later times today.
// submit() and the API still reject past times, since typed values bypass the picker anyway.
const minStartsAt = toLocalInput(startOfToday())
const capacity = ref<number | null>(null)

const submitting = ref(false)
const error = ref('')
const fieldErrors = ref<Record<string, string[]>>({})

const template = computed(() => templates.value.find((t) => t.id === templateId.value))

onMounted(async () => {
  try {
    templates.value = await api.templates()
    templateId.value = templates.value[0]?.id ?? ''
  } catch {
    error.value = 'Could not load games. Is the API running?'
  }
})

// Picking a game resets the fields it drives: formats and capacity defaults/limits.
watch(template, (t) => {
  format.value = t?.formats[0] ?? ''
  capacity.value = t?.defaultCapacity ?? null
})

async function submit() {
  error.value = ''
  fieldErrors.value = {}
  const start = new Date(startsAt.value)
  if (Number.isNaN(start.getTime()) || start <= new Date()) {
    error.value = 'Please fix the highlighted fields.'
    fieldErrors.value = {
      StartsAt: [Number.isNaN(start.getTime()) ? 'Choose a date and start time.' : 'Start time must be in the future.'],
    }
    return
  }

  submitting.value = true
  try {
    const created = await api.createEvent({
      name: name.value,
      gameTemplateId: templateId.value,
      format: format.value,
      startsAt: start.toISOString(),
      // A cleared number input yields "", which means "use the game's default".
      capacity: typeof capacity.value === 'number' ? capacity.value : null,
    })
    await router.push(`/events/${created.id}`)
  } catch (e) {
    if (e instanceof ApiError) {
      error.value = 'Please fix the highlighted fields.'
      fieldErrors.value = e.fieldErrors
    } else {
      error.value = 'Something went wrong. Please try again.'
    }
  } finally {
    submitting.value = false
  }
}

// API field keys are PascalCase (e.g. "Capacity").
function fieldError(field: string) {
  const key = Object.keys(fieldErrors.value).find((k) => k.toLowerCase() === field.toLowerCase())
  return key ? fieldErrors.value[key]?.join(' ') : undefined
}

function startOfToday() {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  return d
}

function tomorrowAt(hour: number) {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  d.setHours(hour, 0, 0, 0)
  return d
}

// <input type="datetime-local"> wants local "YYYY-MM-DDTHH:mm" with no zone.
function toLocalInput(d: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}
</script>

<template>
  <h1>Create event</h1>

  <form novalidate @submit.prevent="submit">
    <label>
      Event name
      <input
        v-model="name"
        required
        maxlength="200"
        :aria-invalid="!!fieldError('name')"
        aria-describedby="name-error"
      />
      <span v-if="fieldError('name')" id="name-error" class="error">{{ fieldError('name') }}</span>
    </label>

    <label>
      Game
      <select v-model="templateId" :aria-invalid="!!fieldError('gameTemplateId')" aria-describedby="game-error">
        <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }}</option>
      </select>
      <span v-if="fieldError('gameTemplateId')" id="game-error" class="error">{{ fieldError('gameTemplateId') }}</span>
    </label>

    <label>
      Format
      <select v-model="format" :aria-invalid="!!fieldError('format')" aria-describedby="format-error">
        <option v-for="f in template?.formats ?? []" :key="f" :value="f">{{ f }}</option>
      </select>
      <span v-if="fieldError('format')" id="format-error" class="error">{{ fieldError('format') }}</span>
    </label>

    <label>
      Date and start time
      <input
        v-model="startsAt"
        type="datetime-local"
        :min="minStartsAt"
        required
        :aria-invalid="!!fieldError('startsAt')"
        aria-describedby="starts-hint starts-error"
      />
      <span v-if="template" id="starts-hint" class="hint">
        {{ template.name }} events run {{ template.defaultDurationMinutes / 60 }} hours.
      </span>
      <span v-if="fieldError('startsAt')" id="starts-error" class="error">{{ fieldError('startsAt') }}</span>
    </label>

    <label>
      Player capacity
      <input
        v-model.number="capacity"
        type="number"
        :min="template?.minPlayers"
        :max="template?.maxCapacity"
        required
        :aria-invalid="!!fieldError('capacity')"
        aria-describedby="capacity-hint capacity-error"
      />
      <span v-if="template" id="capacity-hint" class="hint">
        {{ template.minPlayers }}–{{ template.maxCapacity }} players for {{ template.name }}.
      </span>
      <span v-if="fieldError('capacity')" id="capacity-error" class="error">{{ fieldError('capacity') }}</span>
    </label>

    <p class="error" role="alert">{{ error }}</p>

    <div>
      <button type="submit" :disabled="submitting || !template">
        {{ submitting ? 'Creating…' : 'Create event' }}
      </button>
    </div>
  </form>
</template>
