<script setup lang="ts">
import AuthorizedLayout from '../components/authorized/AuthorizedLayout.vue';
import CheckingLayout from '../components/checking/CheckingLayout.vue';
import UnauthorizedLayout from '../components/unauthorized/UnauthorizedLayout.vue';
import Switcher from '../core/components/Switcher.vue';
import { useAuthStore, type AuthStatus } from '../auth.ts';
import { computed } from 'vue';

const authStore = useAuthStore();
const state = computed(() => authStore.$state.status);
function asNumber(status: AuthStatus) {
  switch (status) {
    case 'checking':
      return 0;
    case 'authenticated':
      return 1;
    case 'unauthenticated':
      return 2;
    case 'error':
    default:
      return 3;
  }
}
</script>

<template>
  <Switcher :index="asNumber(state)">
    <span>
      <CheckingLayout />
    </span>
    <span>
      <UnauthorizedLayout />
    </span>
    <span>
      <AuthorizedLayout />
    </span>
  </Switcher>
</template>
