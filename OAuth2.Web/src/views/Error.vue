<script setup lang="ts">
import UnauthorizedLayout from '../layouts/UnauthorizedLayout.vue';
import Error from '../core/components/Error.vue';
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const message = computed(() => {
  const value = route.query.message as string;
  return value;
});
</script>

<style lang="css">
</style>

<template>
  <UnauthorizedLayout>
    <Error :message="message" />
  </UnauthorizedLayout>
</template>