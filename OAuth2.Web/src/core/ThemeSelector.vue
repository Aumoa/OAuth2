<script setup lang="ts">
import { ref } from 'vue';
import Switcher from './components/Switcher.vue';

type Theme = 'light' | 'dark';

function getInitialTheme(): Theme {
  const currentTheme = document.documentElement.dataset.theme;

  if (currentTheme === 'light' || currentTheme === 'dark') {
    return currentTheme;
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

const theme = ref<Theme>(getInitialTheme());

function changeTheme(nextTheme: Theme) {
  theme.value = nextTheme;
  document.documentElement.dataset.theme = nextTheme;
}

changeTheme(theme.value);
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
