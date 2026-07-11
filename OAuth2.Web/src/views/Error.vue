<script setup lang="ts">
import UnauthenticatedLayout from '../components/layout/UnauthenticatedLayout.vue';
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
  <UnauthenticatedLayout>
    <Error :message="message" />
  </UnauthenticatedLayout>
</template>