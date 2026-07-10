<script setup lang="ts">
import { ref } from 'vue';
import Switcher from './components/Switcher.vue';

const theme = ref('light');

function changeTheme(changeTheme: string) {
  theme.value = changeTheme;
}
</script>

<style scoped>
</style>

<template>
  <span>
    <Switcher :index="theme === 'light' ? 0 : 1">
      <button class="icon-button" @click="() => changeTheme('dark')">
        <span class="material-symbols-outlined">dark_mode</span>
      </button>
      <button class="icon-button" @click="() => changeTheme('light')">
        <span class="material-symbols-outlined">light_mode</span>
      </button>
    </Switcher>
  </span>
</template>