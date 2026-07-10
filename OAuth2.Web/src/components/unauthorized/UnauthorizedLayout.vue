<script setup lang="ts">
import LanguageSelector from '../../core/components/LanguageSelector.vue';
import ThemeSelector from '../../core/components/ThemeSelector.vue';
import { useI18n } from 'vue-i18n';

const { t } = useI18n({ useScope: 'global' });
</script>

<style scoped>
</style>

<template>
  <div style="margin: 20px; display: flex; justify-content: flex-end;">
    <LanguageSelector />
    <ThemeSelector />
  </div>
  <span>{{ t('app.state.unauthorized') }}</span>
</template>
