<script setup lang="ts">
import LanguageSelector from '../../core/components/LanguageSelector.vue';
import ThemeSelector from '../../core/components/ThemeSelector.vue';

</script>

<style scoped>
</style>

<template>
  <div style="margin: 20px; display: flex; justify-content: flex-end;">
    <LanguageSelector />
    <ThemeSelector />
  </div>
  <span>Unauthorized</span>
</template>