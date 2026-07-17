<script setup lang="ts">
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import SidebarMainButton from './SidebarMainButton.vue';

interface TransientNavigationButton {
  path: string;
  icon: string;
  label: string;
  indentLevel: number;
  tone: 'default' | 'organization';
}

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const mainButtonHeight = 44;
const navigationGap = 4;
const focusedButtonIndex = ref<number | null>(null);
const organizationId = computed(() => {
  const value = route.params.organizationId;
  if (value === undefined) {
    return undefined;
  }

  return Array.isArray(value) ? value.join('/') : value;
});
const applicationGroupButton = computed<TransientNavigationButton | null>(() => {
  if (typeof route.name === 'string' && route.name.startsWith('applications-personal')) {
    return {
      path: router.resolve({ name: 'applications-personal' }).path,
      icon: 'person',
      label: t('app.sidebar.personalApplications'),
      indentLevel: 1,
      tone: 'default',
    };
  }

  if (
    typeof route.name === 'string'
    && route.name.startsWith('applications-organization')
    && organizationId.value !== undefined
  ) {
    return {
      path: router.resolve({
        name: 'applications-organization',
        params: { organizationId: organizationId.value },
      }).path,
      icon: 'domain',
      label: t('app.sidebar.organizationApplications', {
        organization: organizationId.value,
      }),
      indentLevel: 1,
      tone: 'organization',
    };
  }

  return null;
});
const applicationLeafButton = computed<TransientNavigationButton | null>(() => {
  if (
    route.name === 'applications-personal-new'
    || route.name === 'applications-organization-new'
  ) {
    return {
      path: route.path,
      icon: 'add_circle',
      label: t('app.sidebar.newApplication'),
      indentLevel: 1,
      tone: organizationId.value === undefined ? 'default' : 'organization',
    };
  }

  if (
    route.name === 'applications-personal-edit'
    || route.name === 'applications-organization-edit'
  ) {
    const clientIdParam = route.params.clientId;
    const clientId = Array.isArray(clientIdParam) ? clientIdParam.join('/') : clientIdParam;
    return {
      path: route.path,
      icon: 'edit',
      label: t('app.sidebar.editApplication', { clientId }),
      indentLevel: 1,
      tone: organizationId.value === undefined ? 'default' : 'organization',
    };
  }

  return null;
});
const transientApplicationButtons = computed(() => [
  applicationGroupButton.value,
  applicationLeafButton.value,
].filter((button): button is TransientNavigationButton => button !== null));
const navigationPaths = computed(() => [
  '/',
  '/applications',
  ...transientApplicationButtons.value.map(button => button.path),
]);
const activeButtonIndex = computed(() => {
  const exactIndex = navigationPaths.value.indexOf(route.path);
  if (exactIndex >= 0) {
    return exactIndex;
  }

  return navigationPaths.value.findIndex(path => (
    path !== '/' && route.path.startsWith(`${path}/`)
  ));
});
const highlightedButtonIndex = computed(() => {
  const focusedIndex = focusedButtonIndex.value;
  return focusedIndex !== null
    && focusedIndex >= 0
    && focusedIndex < navigationPaths.value.length
    ? focusedIndex
    : activeButtonIndex.value;
});
const highlightedButton = computed(() => (
  highlightedButtonIndex.value >= 2
    ? transientApplicationButtons.value[highlightedButtonIndex.value - 2]
    : undefined
));
const highlightedButtonIndentLevel = computed(() => highlightedButton.value?.indentLevel ?? 0);
const navigationStyle = computed(() => ({
  '--sidebar-main-button-height': `${mainButtonHeight}px`,
  '--sidebar-navigation-gap': `${navigationGap}px`,
  '--sidebar-active-button-indent': `${highlightedButtonIndentLevel.value * 20}px`,
  '--sidebar-active-button-offset': `${Math.max(highlightedButtonIndex.value, 0)
    * (mainButtonHeight + navigationGap)}px`,
  '--sidebar-highlight-border': highlightedButton.value?.tone === 'organization'
    ? 'color-mix(in srgb, #a78bfa 52%, var(--border))'
    : 'var(--accent-border)',
  '--sidebar-highlight-background': highlightedButton.value?.tone === 'organization'
    ? 'color-mix(in srgb, #8b5cf6 13%, transparent)'
    : 'var(--accent-bg)',
  '--sidebar-highlight-accent': highlightedButton.value?.tone === 'organization'
    ? '#8b5cf6'
    : 'var(--accent)',
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
  left: var(--sidebar-active-button-indent, 0px);
  width: calc(100% - var(--sidebar-active-button-indent, 0px));
  height: var(--sidebar-main-button-height);
  box-sizing: border-box;
  border: 1px solid var(--sidebar-highlight-border, var(--accent-border));
  border-radius: 9px;
  background: var(--sidebar-highlight-background, var(--accent-bg));
  box-shadow: inset 3px 0 0 var(--sidebar-highlight-accent, var(--accent));
  opacity: 0;
  pointer-events: none;
  transform: translateY(var(--sidebar-active-button-offset, 0));
  transition:
    opacity 120ms ease,
    border-color 180ms ease,
    background-color 180ms ease,
    box-shadow 180ms ease,
    left 220ms cubic-bezier(0.22, 1, 0.36, 1),
    width 220ms cubic-bezier(0.22, 1, 0.36, 1),
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
      <SidebarMainButton
        v-for="(button, index) in transientApplicationButtons"
        :key="button.path"
        :icon="button.icon"
        :label="button.label"
        :to="button.path"
        :indent-level="button.indentLevel"
        :tone="button.tone"
        @focus="focusButton(index + 2)"
        @blur="blurButton(index + 2)"
      />
    </nav>
  </div>
</template>
