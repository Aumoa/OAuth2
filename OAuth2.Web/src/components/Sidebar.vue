<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import SidebarMainButton from './SidebarMainButton.vue';

const { t } = useI18n();
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

.sidebar-navigation {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
</style>

<template>
  <div class="sidebar">
    <nav class="sidebar-navigation" :aria-label="t('app.sidebar.navigationLabel')">
      <SidebarMainButton
        icon="account_circle"
        :label="t('app.sidebar.accountInformation')"
        to="/"
      />
    </nav>
  </div>
</template>
