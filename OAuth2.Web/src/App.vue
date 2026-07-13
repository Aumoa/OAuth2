<script setup lang="ts">
import { RouterView } from 'vue-router';
import { theme } from './core/scripts/theme.ts';
import { useAuthStore } from './auth.ts';
import { requiredAuthenticated, router } from './router/index.ts';

document.documentElement.dataset.theme = theme.value;

router.beforeEach(async to => {
  if (!requiredAuthenticated(to.path)) {
    return true;
  }

  const auth = useAuthStore();

  try {
    await auth.initializeAsync();
  } catch {
    return { name: 'error' };
  }

  if (!auth.isAuthenticated) {
    return {
      name: 'login',
      query: {
        returnUrl: to.fullPath,
      },
    };
  }

  return true;
});
</script>

<template>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
  <RouterView />
</template>
