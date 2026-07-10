<script setup lang="ts">
import { ref } from 'vue';

const theme = ref('light');

function changeTheme() {
  document.documentElement.setAttribute('data-theme', theme.value);
}
</script>

<style scoped>
</style>

<template>
  <span>
    <button>
    </button>
    <select v-model="theme" @change="changeTheme">
      <option value="light">light</option>
      <option value="dark">dark</option>
    </select>
  </span>
</template>