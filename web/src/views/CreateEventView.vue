<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api, ApiError, type GameTemplate } from '@/api'

const router = useRouter()

const templates = ref<GameTemplate[]>([])
const name = ref('')
const templateId = ref('')
const format = ref('')
// Separate date and time inputs: a date-only picker with min=today reliably allows today,
// while datetime-local pickers grey today out depending on the entered time. submit() and the
// API still reject start times that have already passed.
const date = ref(toDateInput(tomorrow()))
const time = ref('18:00')
const minDate = toDateInput(new Date())
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
  // "YYYY-MM-DDTHH:mm" with no zone parses as local time.
  const start = new Date(`${date.value}T${time.value}`)
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

function tomorrow() {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  return d
}

// <input type="date"> wants the local date as "YYYY-MM-DD".
function toDateInput(d: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
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

    <div class="when">
      <label>
        Date
        <input
          v-model="date"
          type="date"
          :min="minDate"
          required
          :aria-invalid="!!fieldError('startsAt')"
          aria-describedby="starts-error"
        />
      </label>
      <label>
        Start time
        <input
          v-model="time"
          type="time"
          required
          :aria-invalid="!!fieldError('startsAt')"
          aria-describedby="starts-hint starts-error"
        />
      </label>
      <span v-if="template" id="starts-hint" class="hint">
        {{ template.name }} events run {{ template.defaultDurationMinutes / 60 }} hours.
      </span>
      <span v-if="fieldError('startsAt')" id="starts-error" class="error">{{ fieldError('startsAt') }}</span>
    </div>

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

<style scoped>
.when {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.25rem 1rem;
}
.when .hint,
.when .error {
  grid-column: 1 / -1;
}
</style>
