<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n({ useScope: 'global' });
</script>

<style scoped>
</style>

<template>
  <span>{{ t('app.state.loading') }}</span>
</template>
