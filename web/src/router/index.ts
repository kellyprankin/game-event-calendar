import { createRouter, createWebHistory } from 'vue-router'
import CalendarView from '@/views/CalendarView.vue'
import CreateEventView from '@/views/CreateEventView.vue'
import EventView from '@/views/EventView.vue'
import RegisterView from '@/views/RegisterView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'calendar', component: CalendarView },
    { path: '/events/new', name: 'create-event', component: CreateEventView },
    { path: '/events/:id', name: 'event', component: EventView, props: true },
    // The QR code encodes this path; keep it in sync with RegistrationLinks on the API.
    { path: '/events/:id/register', name: 'register', component: RegisterView, props: true },
  ],
})

export default router
