<script setup lang="ts">
import Switcher from '../components/Switcher.vue';
import { theme, setTheme } from '../scripts/theme.ts';

function changeTheme(nextTheme: 'light' | 'dark') {
  setTheme(nextTheme);
}
</script>

<style scoped>
</style>

<template>
  <span>
    <Switcher :index="theme === 'light' ? 0 : 1">
      <button type="button" class="icon-button" aria-label="Use dark theme" @click="changeTheme('dark')">
        <span class="material-symbols-outlined">dark_mode</span>
      </button>
      <button type="button" class="icon-button" aria-label="Use light theme" @click="changeTheme('light')">
        <span class="material-symbols-outlined">light_mode</span>
      </button>
    </Switcher>
  </span>
</template>
