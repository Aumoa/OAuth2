<script setup lang="ts">
import ThemeSelector from '../core/components/ThemeSelector.vue';
import LanguageSelector from '../core/components/LanguageSelector.vue';

</script>

<style lang="css">
.login-form {
  width: min(760px, 100%);
  box-sizing: border-box;
  padding: 32px;
  border: 1px solid var(--border);
  border-radius: 8px;
  box-shadow: var(--shadow);
  background: color-mix(in srgb, var(--bg) 70%, transparent);
  backdrop-filter: blur(18px) saturate(120%);
  -webkit-backdrop-filter: blur(18px) saturate(120%);
}

.root-panel {
  flex: 1;
  place-items: center;
  flex-direction: column;
  align-content: center;
}

.control-panel {
  display: flex;
  flex-direction: row;
  align-self: flex-end;
  gap: 10px;
  margin: 5px;
}
</style>

<template>
  <div class="control-panel">
    <LanguageSelector />
    <ThemeSelector />
  </div>
  <div class="root-panel">
    <div class="login-form">
    </div>
  </div>
</template>