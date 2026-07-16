<script setup lang="ts">
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import SidebarMainButton from './SidebarMainButton.vue';

const { t } = useI18n();
const route = useRoute();
const navigationPaths = ['/', '/applications'] as const;
const mainButtonHeight = 44;
const navigationGap = 4;
const focusedButtonIndex = ref<number | null>(null);
const activeButtonIndex = computed(() => navigationPaths.findIndex(path => (
  route.path === path || (path !== '/' && route.path.startsWith(`${path}/`))
)));
const highlightedButtonIndex = computed(() => (
  focusedButtonIndex.value ?? activeButtonIndex.value
));
const navigationStyle = computed(() => ({
  '--sidebar-main-button-height': `${mainButtonHeight}px`,
  '--sidebar-navigation-gap': `${navigationGap}px`,
  '--sidebar-active-button-offset': `${Math.max(highlightedButtonIndex.value, 0)
    * (mainButtonHeight + navigationGap)}px`,
}));

function focusButton(index: number): void {
  focusedButtonIndex.value = index;
}

function blurButton(index: number): void {
  if (focusedButtonIndex.value === index) {
    focusedButtonIndex.value = null;
  }
}
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
  --sidebar-main-button-height: 44px;
  --sidebar-navigation-gap: 4px;

  position: relative;
  display: flex;
  flex-direction: column;
  gap: var(--sidebar-navigation-gap);
}

.sidebar-navigation-highlight {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: var(--sidebar-main-button-height);
  box-sizing: border-box;
  border: 1px solid var(--accent-border);
  border-radius: 9px;
  background: var(--accent-bg);
  box-shadow: inset 3px 0 0 var(--accent);
  opacity: 0;
  pointer-events: none;
  transform: translateY(var(--sidebar-active-button-offset, 0));
  transition:
    opacity 120ms ease,
    transform 220ms cubic-bezier(0.22, 1, 0.36, 1);
  will-change: transform;
}

.sidebar-navigation.has-active-button .sidebar-navigation-highlight {
  opacity: 1;
}

@media (prefers-reduced-motion: reduce) {
  .sidebar-navigation-highlight {
    transition-duration: 0.01ms;
  }
}
</style>

<template>
  <div class="sidebar">
    <nav
      class="sidebar-navigation"
      :class="{ 'has-active-button': highlightedButtonIndex >= 0 }"
      :style="navigationStyle"
      :aria-label="t('app.sidebar.navigationLabel')"
    >
      <span class="sidebar-navigation-highlight" aria-hidden="true"></span>

      <SidebarMainButton
        icon="account_circle"
        :label="t('app.sidebar.accountInformation')"
        to="/"
        @focus="focusButton(0)"
        @blur="blurButton(0)"
      />
      <SidebarMainButton
        icon="apps"
        :label="t('app.sidebar.applicationManagement')"
        to="/applications"
        @focus="focusButton(1)"
        @blur="blurButton(1)"
      />
    </nav>
  </div>
</template>
