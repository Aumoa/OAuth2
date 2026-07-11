<script setup lang="ts">
import { RouterView } from 'vue-router';
import { theme } from './core/scripts/theme.ts';
import { useAuthStore } from './auth.ts';
import { router } from './router/index.ts';
import { RouteError } from './router/route-error.ts';

async function initializeAsync() {
  if (authStore.$state.status === 'checking') {
    router.push('/checking');
  }
  try {
    await authStore.initializeAsync();
  }
  catch (error) {
    if (error instanceof RouteError) {
      const routeError = error as RouteError;
      router.replace(routeError.url);
    }
    else {
      router.replace('/error');
    }
  }
}

const authStore = useAuthStore();

document.documentElement.dataset.theme = theme.value;
initializeAsync();
</script>

<template>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
  <RouterView />
</template>
