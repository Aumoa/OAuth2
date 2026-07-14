<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, useId } from 'vue';
import { useI18n } from 'vue-i18n';
import Avatar from './Avatar.vue';
import { useAuthStore } from '../src/auth.ts';

const props = withDefaults(defineProps<{
  accountManagementUrl?: string;
  loginUrl?: string;
}>(), {
  accountManagementUrl: '/',
  loginUrl: '/api/v1/auth/login',
});

const auth = useAuthStore();
const { t } = useI18n({ useScope: 'global' });
const container = ref<HTMLElement | null>(null);
const trigger = ref<HTMLButtonElement | null>(null);
const isPinnedOpen = ref(false);
const isHoverOpen = ref(false);
const isLoggingOut = ref(false);
const logoutError = ref<string | null>(null);
const copyState = ref<'idle' | 'copied' | 'error'>('idle');
const panelId = `profile-card-${useId()}`;

let copyStateTimer: ReturnType<typeof setTimeout> | undefined;
let hoverCloseTimer: ReturnType<typeof setTimeout> | undefined;

const isOpen = computed(() => isPinnedOpen.value || isHoverOpen.value);
const displayName = computed(() => (
  auth.user?.name
  ?? auth.user?.id
  ?? auth.user?.sub
  ?? t('oauth2.profileCard.unknownUser')
));
const email = computed(() => auth.user?.email ?? null);
const subject = computed(() => auth.user?.sub ?? null);
const copyLabel = computed(() => {
  if (copyState.value === 'copied') {
    return t('oauth2.profileCard.copied');
  }

  if (copyState.value === 'error') {
    return t('oauth2.profileCard.copyFailed');
  }

  return t('oauth2.profileCard.copySubject');
});

function toggle(): void {
  isPinnedOpen.value = !isPinnedOpen.value;
  logoutError.value = null;
}

function close(): void {
  clearTimeout(hoverCloseTimer);
  isPinnedOpen.value = false;
  isHoverOpen.value = false;
}

function onMouseEnter(): void {
  clearTimeout(hoverCloseTimer);
  isHoverOpen.value = true;
}

function onMouseLeave(): void {
  clearTimeout(hoverCloseTimer);
  hoverCloseTimer = setTimeout(() => {
    isHoverOpen.value = false;
  }, 140);
}

function onDocumentPointerDown(event: PointerEvent): void {
  if (!container.value?.contains(event.target as Node)) {
    close();
  }
}

function onDocumentKeydown(event: KeyboardEvent): void {
  if (event.key !== 'Escape' || !isOpen.value) {
    return;
  }

  close();
  trigger.value?.focus();
}

function resetCopyStateLater(): void {
  clearTimeout(copyStateTimer);
  copyStateTimer = setTimeout(() => {
    copyState.value = 'idle';
  }, 1800);
}

async function copySubjectAsync(): Promise<void> {
  if (!subject.value) {
    return;
  }

  try {
    await navigator.clipboard.writeText(subject.value);
    copyState.value = 'copied';
  } catch {
    copyState.value = 'error';
  }

  resetCopyStateLater();
}

async function logoutAsync(): Promise<void> {
  if (isLoggingOut.value) {
    return;
  }

  isLoggingOut.value = true;
  logoutError.value = null;

  try {
    await auth.logoutAsync();
    window.location.replace(props.loginUrl);
  } catch {
    logoutError.value = t('oauth2.profileCard.logoutFailed');
  } finally {
    isLoggingOut.value = false;
  }
}

onMounted(() => {
  document.addEventListener('pointerdown', onDocumentPointerDown);
  document.addEventListener('keydown', onDocumentKeydown);
});

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocumentPointerDown);
  document.removeEventListener('keydown', onDocumentKeydown);
  clearTimeout(copyStateTimer);
  clearTimeout(hoverCloseTimer);
});
</script>

<style scoped lang="css">
.profile-card {
  position: relative;
  display: inline-grid;
  place-items: center;
}

.profile-trigger {
  display: grid;
  width: 36px;
  height: 36px;
  padding: 2px;
  color: inherit;
  background: transparent;
  cursor: pointer;
  transition: background-color 0.15s ease, box-shadow 0.15s ease;
}

.profile-trigger:hover,
.profile-trigger[aria-expanded='true'] {
  background: var(--surface-muted);
}

.profile-trigger:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
}

.profile-panel {
  position: absolute;
  top: calc(100% + 9px);
  right: 0;
  z-index: 1000;
  width: min(310px, calc(100vw - 24px));
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: 9px;
  color: var(--text);
  background: color-mix(in srgb, var(--surface) 96%, transparent);
  box-shadow: var(--shadow-md);
  transform-origin: top right;
  backdrop-filter: blur(14px);
}

.profile-summary {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 14px;
  align-items: center;
  padding: 16px;
}

.profile-identity {
  display: flex;
  min-width: 0;
  flex-direction: column;
  justify-content: center;
  text-align: left;
  gap: 3px;
}

.profile-metadata {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 1px;
  margin-top: 3px;
}

.profile-name,
.profile-detail,
.profile-subject-value {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.profile-name {
  color: var(--text-h);
  font-size: 15px;
  font-weight: 700;
  line-height: 1.35;
}

.profile-detail {
  color: var(--text-muted);
  font-size: 12px;
  line-height: 1.35;
}

.profile-subject {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 20px;
  gap: 4px;
  align-items: center;
  min-height: 20px;
}

.profile-subject-value {
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 11px;
}

.copy-button {
  display: grid;
  width: 20px;
  height: 20px;
  padding: 0;
  place-items: center;
  border: 0;
  border-radius: 5px;
  color: var(--text-muted);
  background: transparent;
  cursor: pointer;
}

.copy-button:hover {
  color: var(--text-h);
  background: var(--surface-muted);
}

.copy-button:focus-visible,
.profile-action:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: -2px;
}

.copy-button .material-symbols-outlined {
  font-size: 15px;
}

.profile-actions {
  display: flex;
  flex-direction: column;
  padding: 5px 0;
  border-top: 1px solid var(--border);
}

.profile-action {
  display: grid;
  grid-template-columns: 22px minmax(0, 1fr);
  gap: 8px;
  align-items: center;
  min-height: 44px;
  padding: 0 16px;
  border: 0;
  color: var(--text);
  background: transparent;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  text-align: left;
  text-decoration: none;
  cursor: pointer;
  transition: color 0.15s ease, background-color 0.15s ease;
}

.profile-action:hover {
  color: var(--text-h);
  background: var(--surface-muted);
}

.profile-action.logout:hover {
  color: var(--danger);
  background: var(--danger-bg);
}

.profile-action:disabled {
  cursor: wait;
  opacity: 0.65;
}

.profile-action .material-symbols-outlined {
  font-size: 20px;
}

.profile-error {
  margin: 0;
  padding: 8px 16px 12px;
  border-top: 1px solid var(--border);
  color: var(--danger);
  font-size: 12px;
  line-height: 1.4;
}

.profile-card-panel-enter-active,
.profile-card-panel-leave-active {
  transition: opacity 0.16s ease, transform 0.16s ease;
}

.profile-card-panel-enter-from,
.profile-card-panel-leave-to {
  opacity: 0;
  transform: translateY(-6px) scale(0.98);
}

@media (prefers-reduced-motion: reduce) {
  .profile-trigger,
  .profile-action,
  .profile-card-panel-enter-active,
  .profile-card-panel-leave-active {
    transition-duration: 0.01ms;
  }
}
</style>

<template>
  <div
    ref="container"
    class="profile-card"
    @mouseenter="onMouseEnter"
    @mouseleave="onMouseLeave"
  >
    <button
      ref="trigger"
      type="button"
      class="app-button profile-trigger"
      :aria-label="t('oauth2.profileCard.openMenu')"
      aria-haspopup="dialog"
      :aria-expanded="isOpen"
      :aria-controls="panelId"
      @click="toggle"
    >
      <Avatar size="small" />
    </button>

    <Transition name="profile-card-panel">
      <section
        v-if="isOpen"
        :id="panelId"
        class="profile-panel"
        role="dialog"
        :aria-label="t('oauth2.profileCard.menuLabel')"
      >
        <div class="profile-summary">
          <Avatar size="large" />

          <div class="profile-identity">
            <strong class="profile-name" :title="displayName">{{ displayName }}</strong>

            <div v-if="email || subject" class="profile-metadata">
              <span v-if="email" class="profile-detail" :title="email">{{ email }}</span>

              <div v-if="subject" class="profile-subject">
                <span class="profile-detail profile-subject-value" :title="subject">{{ subject }}</span>
                <button
                  type="button"
                  class="copy-button"
                  :aria-label="copyLabel"
                  :title="copyLabel"
                  @click="copySubjectAsync"
                >
                  <span class="material-symbols-outlined" aria-hidden="true">
                    {{ copyState === 'copied' ? 'check' : copyState === 'error' ? 'error' : 'content_copy' }}
                  </span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <nav class="profile-actions" :aria-label="t('oauth2.profileCard.actionsLabel')">
          <a class="profile-action" :href="props.accountManagementUrl" @click="close">
            <span class="material-symbols-outlined" aria-hidden="true">manage_accounts</span>
            <span>{{ t('oauth2.profileCard.accountManagement') }}</span>
          </a>

          <button
            type="button"
            class="profile-action logout"
            :disabled="isLoggingOut"
            @click="logoutAsync"
          >
            <span class="material-symbols-outlined" aria-hidden="true">logout</span>
            <span>
              {{ isLoggingOut ? t('oauth2.profileCard.loggingOut') : t('oauth2.profileCard.logout') }}
            </span>
          </button>
        </nav>

        <p v-if="logoutError" class="profile-error" role="alert">{{ logoutError }}</p>
      </section>
    </Transition>
  </div>
</template>
