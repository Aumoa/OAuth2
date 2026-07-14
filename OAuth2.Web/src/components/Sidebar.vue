<script setup lang="ts">
import SidebarMainButton from './SidebarMainButton.vue';

</script>

<style lang="css">
.sidebar {
  border-right: 1px solid var(--border-strong);
  width: 100%;
  height: 100%;
  background: color-mix(in srgb, var(--surface-muted) 50%, transparent);
  display: flex;
  flex-direction: column;
  padding: 10px;
  box-sizing: border-box;
}
</style>

<template>
  <div class="sidebar">
    <SidebarMainButton />
  </div>
</template>
