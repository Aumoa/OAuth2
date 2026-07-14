<script setup lang="ts">
import Error from '../core/components/Error.vue';
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';

const route = useRoute();
const { t } = useI18n();

const message = computed(() => {
  const value = route.query.message as string;
  return value ?? t('app.common.errorMessages.unexpected');
});
</script>

<template>
  <Error :message="message" />
</template>