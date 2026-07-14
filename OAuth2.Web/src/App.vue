<script setup lang="ts">
import { RouterView } from 'vue-router';
import { theme } from './core/scripts/theme.ts';
import { useAuthStore } from './shared/oauth2/src/auth.ts';
import { requiresAuthentication, router } from './router/index.ts';

document.documentElement.dataset.theme = theme.value;

router.beforeEach(async to => {
  if (!requiresAuthentication(to)) {
    return true;
  }

  const auth = useAuthStore();

  try {
    await auth.initializeAsync();
  } catch {
    return { name: 'error' };
  }

  if (!auth.isAuthenticated) {
    window.location.replace('/api/v1/auth/login');
    return false;
  }

  return true;
});
</script>

<template>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
  <RouterView />
</template>
