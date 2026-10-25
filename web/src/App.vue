<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <header>
    <RouterLink to="/" class="brand">Game Events</RouterLink>
    <nav aria-label="Main">
      <RouterLink to="/">Calendar</RouterLink>
      <RouterLink to="/events/new">Create event</RouterLink>
    </nav>
  </header>
  <main>
    <RouterView />
  </main>
</template>

<style>
:root {
  --fg: #1b1b1f;
  --muted: #5c5c66;
  --bg: #ffffff;
  --surface: #f4f4f6;
  --border: #d6d6dc;
  --accent: #6b2fb3;
  --danger: #b3261e;
  --ok: #1e7a3a;
  color-scheme: light dark;
}
@media (prefers-color-scheme: dark) {
  :root {
    --fg: #ececf1;
    --muted: #a5a5b0;
    --bg: #16161a;
    --surface: #222228;
    --border: #3a3a44;
    --accent: #b48cf0;
    --danger: #f2827a;
    --ok: #6fd18c;
  }
}
* { box-sizing: border-box; }
body {
  margin: 0;
  font-family: system-ui, sans-serif;
  line-height: 1.5;
  color: var(--fg);
  background: var(--bg);
}
header {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 1.5rem;
  align-items: center;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--border);
}
header nav { display: flex; gap: 1rem; }
.brand { font-weight: 700; font-size: 1.1rem; }
main { max-width: 48rem; margin: 0 auto; padding: 1rem; }
a { color: var(--accent); }
h1 { font-size: 1.6rem; margin: 0.5rem 0 1rem; }
.muted { color: var(--muted); }
.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 1rem;
}
.error { color: var(--danger); }
.success { color: var(--ok); }
form { display: grid; gap: 1rem; max-width: 28rem; }
label { display: grid; gap: 0.25rem; font-weight: 600; }
.hint { font-weight: 400; font-size: 0.875rem; color: var(--muted); }
input, select, button { font: inherit; }
input, select {
  padding: 0.5rem;
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--bg);
  color: var(--fg);
}
button, .button {
  display: inline-block;
  padding: 0.55rem 1rem;
  border: 0;
  border-radius: 6px;
  background: var(--accent);
  color: var(--bg);
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
}
button:disabled { opacity: 0.6; cursor: not-allowed; }
:focus-visible { outline: 3px solid var(--accent); outline-offset: 2px; }
</style>
