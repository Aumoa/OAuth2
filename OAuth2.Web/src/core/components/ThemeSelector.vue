<script setup lang="ts">
import Switcher from '../components/Switcher.vue';
import { theme, setTheme } from '../scripts/theme.ts';
import { useI18n } from 'vue-i18n';

const { t } = useI18n({ useScope: 'global' });

function changeTheme(nextTheme: 'light' | 'dark') {
  setTheme(nextTheme);
}
</script>

<style scoped>
</style>

<template>
  <span>
    <Switcher :index="theme === 'light' ? 0 : 1">
      <button type="button" class="app-button app-icon-button" :aria-label="t('core.themeSelector.useDark')" @click="changeTheme('dark')">
        <span class="material-symbols-outlined">light_mode</span>
      </button>
      <button type="button" class="app-button app-icon-button" :aria-label="t('core.themeSelector.useLight')" @click="changeTheme('light')">
        <span class="material-symbols-outlined">dark_mode</span>
      </button>
    </Switcher>
  </span>
</template>
