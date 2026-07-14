<script setup lang="ts">
import LanguageSelector from '../core/components/LanguageSelector.vue';
import ThemeSelector from '../core/components/ThemeSelector.vue';
</script>

<style lang="css">
.control-panel {
  display: flex;
  flex-direction: row;
  align-self: flex-end;
  gap: 10px;
  margin: 5px;
}

.root-panel {
  flex: 1;
  place-items: center;
  flex-direction: column;
  align-content: center;
}
</style>

<template>
  <div class="control-panel">
    <LanguageSelector />
    <ThemeSelector />
  </div>
  <div class="root-panel">
    <RouterView />
  </div>
</template>