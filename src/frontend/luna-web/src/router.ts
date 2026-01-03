import { createRouter, createWebHistory } from 'vue-router'
import Classroom from '@/components/Classroom.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [{ path: '/', component: Classroom }],
})