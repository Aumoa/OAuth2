<script setup lang="ts">
import { RouterView } from 'vue-router';
import { theme } from './core/scripts/theme.ts';
import { useAuthStore } from './auth.ts';
import { requiredAuthenticated, router } from './router/index.ts';
import { RouteError } from './router/route-error.ts';
import { onMounted } from 'vue';

async function initializeAsync() {
  console.log(router.currentRoute.value.path);
  if (requiredAuthenticated()) {
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
}

const authStore = useAuthStore();

document.documentElement.dataset.theme = theme.value;

onMounted(() => {
  initializeAsync();
});
</script>

<template>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
  <RouterView />
</template>
