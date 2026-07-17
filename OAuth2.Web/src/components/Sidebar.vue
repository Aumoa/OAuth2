<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { Applications } from '../api/applications.ts';
import { useOrganizationsStore } from '../stores/organizations.ts';
import SidebarMainButton from './SidebarMainButton.vue';

interface NavigationButton {
  key: string;
  path: string;
  icon: string;
  label: string;
  indentLevel: number;
  tone: 'default' | 'organization';
}

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const organizationsStore = useOrganizationsStore();
const mainButtonHeight = 44;
const navigationGap = 4;
const sectionDividerHeight = 15;
const focusedButtonKey = ref<string | null>(null);
const editingApplicationName = ref<string | null>(null);
let applicationNameRequestId = 0;
const organizationId = computed(() => {
  const value = route.params.organizationId;
  if (value === undefined) {
    return undefined;
  }

  return Array.isArray(value) ? value.join('/') : value;
});
const editingApplicationId = computed(() => {
  if (
    route.name !== 'applications-personal-edit'
    && route.name !== 'applications-organization-edit'
  ) {
    return undefined;
  }

  const value = route.params.clientId;
  return Array.isArray(value) ? value.join('/') : value;
});
const applicationGroupButton = computed<NavigationButton | null>(() => {
  if (typeof route.name === 'string' && route.name.startsWith('applications-personal')) {
    return {
      key: 'applications-personal',
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
      key: `applications-organization-${organizationId.value}`,
      path: router.resolve({
        name: 'applications-organization',
        params: { organizationId: organizationId.value },
      }).path,
      icon: 'domain',
      label: t('app.sidebar.organizationApplications', {
        organization: organizationsStore.find(organizationId.value)?.name ?? organizationId.value,
      }),
      indentLevel: 1,
      tone: 'organization',
    };
  }

  return null;
});
const applicationLeafButton = computed<NavigationButton | null>(() => {
  if (
    route.name === 'applications-personal-new'
    || route.name === 'applications-organization-new'
  ) {
    return {
      key: `application-new-${route.path}`,
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
    return {
      key: `application-edit-${route.path}`,
      path: route.path,
      icon: 'edit',
      label: editingApplicationName.value === null
        ? t('app.sidebar.editApplicationFallback')
        : t('app.sidebar.editApplication', { name: editingApplicationName.value }),
      indentLevel: 1,
      tone: organizationId.value === undefined ? 'default' : 'organization',
    };
  }

  return null;
});
const transientApplicationButtons = computed(() => [
  applicationGroupButton.value,
  applicationLeafButton.value,
].filter((button): button is NavigationButton => button !== null));
const organizationButtons = computed<NavigationButton[]>(() => {
  const collator = new Intl.Collator(undefined, { sensitivity: 'base' });
  return [...organizationsStore.organizations]
    .sort((left, right) => collator.compare(left.name, right.name))
    .map(organization => ({
      key: `organization-${organization.id}`,
      path: router.resolve({
        name: 'organization-management',
        params: { organizationId: organization.id },
      }).path,
      icon: 'domain',
      label: organization.name,
      indentLevel: 0,
      tone: 'organization',
    }));
});
const primaryNavigationButtons = computed<NavigationButton[]>(() => [
  {
    key: 'account',
    path: '/',
    icon: 'account_circle',
    label: t('app.sidebar.accountInformation'),
    indentLevel: 0,
    tone: 'default',
  },
  {
    key: 'applications',
    path: '/applications',
    icon: 'apps',
    label: t('app.sidebar.applicationManagement'),
    indentLevel: 0,
    tone: 'default',
  },
  ...transientApplicationButtons.value,
  ...organizationButtons.value,
]);
const createOrganizationButton = computed<NavigationButton>(() => ({
  key: 'organization-new',
  path: router.resolve({ name: 'organization-new' }).path,
  icon: 'add_business',
  label: t('app.sidebar.addOrganization'),
  indentLevel: 0,
  tone: 'organization',
}));
const navigationButtons = computed(() => [
  ...primaryNavigationButtons.value,
  createOrganizationButton.value,
]);
const navigationPaths = computed(() => navigationButtons.value.map(button => button.path));
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
  if (focusedButtonKey.value !== null) {
    const focusedIndex = navigationButtons.value.findIndex(
      button => button.key === focusedButtonKey.value,
    );
    if (focusedIndex >= 0) {
      return focusedIndex;
    }
  }

  return activeButtonIndex.value;
});
const highlightedButton = computed(() => navigationButtons.value[highlightedButtonIndex.value]);
const highlightedButtonIndentLevel = computed(() => highlightedButton.value?.indentLevel ?? 0);
const navigationStyle = computed(() => ({
  '--sidebar-main-button-height': `${mainButtonHeight}px`,
  '--sidebar-navigation-gap': `${navigationGap}px`,
  '--sidebar-active-button-indent': `${highlightedButtonIndentLevel.value * 20}px`,
  '--sidebar-active-button-offset': `${Math.max(highlightedButtonIndex.value, 0)
    * (mainButtonHeight + navigationGap)
    + (highlightedButton.value?.key === createOrganizationButton.value.key
      ? sectionDividerHeight + navigationGap
      : 0)}px`,
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

function focusButton(key: string): void {
  focusedButtonKey.value = key;
}

function blurButton(key: string): void {
  if (focusedButtonKey.value === key) {
    focusedButtonKey.value = null;
  }
}

onMounted(() => {
  void organizationsStore.loadAsync().catch(() => undefined);
});

watch(
  [editingApplicationId, organizationId],
  async ([clientId, currentOrganizationId]) => {
    const requestId = ++applicationNameRequestId;
    editingApplicationName.value = null;
    if (clientId === undefined) {
      return;
    }

    try {
      const application = await Applications.getAsync(clientId, currentOrganizationId);
      if (requestId === applicationNameRequestId) {
        editingApplicationName.value = application.name;
      }
    } catch {
      // Keep the client ID as a stable fallback when details cannot be loaded.
    }
  },
  { immediate: true },
);
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

.sidebar-section-divider {
  width: calc(100% - 20px);
  height: 1px;
  margin: 7px 10px;
  flex: 0 0 1px;
  border: 0;
  background: color-mix(in srgb, #a78bfa 22%, var(--border));
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
        v-for="button in primaryNavigationButtons"
        :key="button.key"
        :icon="button.icon"
        :label="button.label"
        :to="button.path"
        :indent-level="button.indentLevel"
        :tone="button.tone"
        @focus="focusButton(button.key)"
        @blur="blurButton(button.key)"
      />

      <hr class="sidebar-section-divider" aria-hidden="true">

      <SidebarMainButton
        :icon="createOrganizationButton.icon"
        :label="createOrganizationButton.label"
        :to="createOrganizationButton.path"
        :tone="createOrganizationButton.tone"
        @focus="focusButton(createOrganizationButton.key)"
        @blur="blurButton(createOrganizationButton.key)"
      />
    </nav>
  </div>
</template>
