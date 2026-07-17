<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter, type RouteLocationRaw } from 'vue-router';
import { Applications, type ApplicationDetails } from '../api/applications.ts';
import Dialog from '../core/components/Dialog.vue';
import FloatingInput from '../core/components/FloatingInput.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

type ViewState = 'loading' | 'ready' | 'error' | 'notFound';
type RedirectUriEntry = {
  id: number;
  value: string;
  initialValue: string | null;
};

const requiredScope = 'openid';
const availableScopes = ['openid', 'profile', 'email', 'address', 'phone', 'groups'] as const;
const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const state = ref<ViewState>('loading');
const application = ref<ApplicationDetails | null>(null);
const redirectUris = ref<RedirectUriEntry[]>([]);
const redirectUriInputs = ref<HTMLInputElement[]>([]);
const allowedScopes = ref<string[]>([]);
const initialRedirectUris = ref<string[]>([]);
const initialAllowedScopes = ref<string[]>([]);
const redirectUrisError = ref<string | null>(null);
const saveError = ref<string | null>(null);
const savedMessage = ref<string | null>(null);
const isSaving = ref(false);
const isDeleteDialogOpen = ref(false);
const deleteConfirmation = ref('');
const deleteError = ref<string | null>(null);
const isDeleting = ref(false);
const deleteConfirmationInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const clientId = computed(() => {
  const value = route.params.clientId;
  return Array.isArray(value) ? value.join('/') : (value ?? '');
});
const organizationId = computed(() => {
  const value = route.params.organizationId;
  if (value === undefined) {
    return undefined;
  }

  return Array.isArray(value) ? value.join('/') : value;
});
const isOrganization = computed(() => organizationId.value !== undefined);
const applicationsListRoute = computed<RouteLocationRaw>(() => organizationId.value === undefined
  ? { name: 'applications-personal' }
  : {
    name: 'applications-organization',
    params: { organizationId: organizationId.value },
  });
const deleteConfirmationMatches = computed(() => (
  application.value !== null
  && deleteConfirmation.value === application.value.name
));
const hasRedirectUriChanges = computed(() => (
  !areStringArraysEqual(normalizedRedirectUris(), initialRedirectUris.value)
));
const hasAllowedScopeChanges = computed(() => (
  availableScopes.some(isScopeChanged)
));
const hasChanges = computed(() => (
  hasRedirectUriChanges.value || hasAllowedScopeChanges.value
));
let isMounted = true;
let loadRequestId = 0;
let nextRedirectUriId = 0;

function createRedirectUriEntry(value = '', initialValue: string | null = null): RedirectUriEntry {
  return {
    id: nextRedirectUriId++,
    value,
    initialValue,
  };
}

function normalizedRedirectUris(): string[] {
  return redirectUris.value
    .map(entry => entry.value.trim())
    .filter(value => value.length > 0);
}

function normalizedAllowedScopes(): string[] {
  return availableScopes.filter(scope => (
    scope === requiredScope || allowedScopes.value.includes(scope)
  ));
}

function areStringArraysEqual(left: string[], right: string[]): boolean {
  return left.length === right.length
    && left.every((value, index) => value === right[index]);
}

function isScopeChanged(scope: string): boolean {
  return allowedScopes.value.includes(scope) !== initialAllowedScopes.value.includes(scope);
}

function isRedirectUriChanged(entry: RedirectUriEntry): boolean {
  if (!hasRedirectUriChanges.value) {
    return false;
  }

  const value = entry.value.trim();
  return entry.initialValue === null
    ? value.length > 0
    : value !== entry.initialValue;
}

function clearSaveFeedback(): void {
  saveError.value = null;
  savedMessage.value = null;
}

function handleRedirectUriInput(): void {
  redirectUrisError.value = null;
  clearSaveFeedback();
}

async function addRedirectUriAsync(): Promise<void> {
  if (redirectUris.value.length >= 20 || isSaving.value || isDeleting.value) {
    return;
  }

  redirectUris.value.push(createRedirectUriEntry());
  redirectUrisError.value = null;
  clearSaveFeedback();
  await nextTick();
  redirectUriInputs.value[redirectUriInputs.value.length - 1]?.focus();
}

function removeRedirectUri(id: number): void {
  redirectUris.value = redirectUris.value.filter(entry => entry.id !== id);
  redirectUrisError.value = null;
  clearSaveFeedback();
}

function isLoopbackHostname(hostname: string): boolean {
  return hostname === 'localhost'
    || hostname === '127.0.0.1'
    || hostname === '[::1]';
}

function validateRedirectUris(redirectUris: string[]): boolean {
  redirectUrisError.value = null;

  if (redirectUris.length > 20) {
    redirectUrisError.value = t('app.applicationManagement.errors.tooManyRedirectUris');
    return false;
  }

  const uniqueRedirectUris = new Set<string>();
  for (const value of redirectUris) {
    if (value.length > 2048) {
      redirectUrisError.value = t('app.applicationManagement.errors.redirectUriTooLong');
      return false;
    }

    let redirectUri: URL;
    try {
      redirectUri = new URL(value);
    } catch {
      redirectUrisError.value = t('app.applicationManagement.errors.invalidRedirectUri');
      return false;
    }

    const isSecure = redirectUri.protocol === 'https:';
    const isLoopbackHttp = redirectUri.protocol === 'http:'
      && isLoopbackHostname(redirectUri.hostname);
    if (
      redirectUri.hostname.length === 0
      || redirectUri.username.length > 0
      || redirectUri.password.length > 0
      || redirectUri.hash.length > 0
      || (!isSecure && !isLoopbackHttp)
    ) {
      redirectUrisError.value = t('app.applicationManagement.errors.invalidRedirectUri');
      return false;
    }

    if (uniqueRedirectUris.has(value)) {
      redirectUrisError.value = t('app.applicationManagement.errors.duplicateRedirectUri');
      return false;
    }

    uniqueRedirectUris.add(value);
  }

  return true;
}

async function loadApplicationAsync(): Promise<void> {
  const requestId = ++loadRequestId;
  state.value = 'loading';
  application.value = null;
  redirectUris.value = [];
  allowedScopes.value = [];
  initialRedirectUris.value = [];
  initialAllowedScopes.value = [];

  try {
    const details = await Applications.getAsync(clientId.value, organizationId.value);
    if (!isMounted || requestId !== loadRequestId) {
      return;
    }

    const loadedScopes = availableScopes.filter(scope => (
      scope === requiredScope || details.allowedScopes.includes(scope)
    ));
    application.value = details;
    redirectUris.value = details.redirectUris.map(value => createRedirectUriEntry(value, value));
    allowedScopes.value = [...loadedScopes];
    initialRedirectUris.value = [...details.redirectUris];
    initialAllowedScopes.value = [...loadedScopes];
    clearSaveFeedback();
    state.value = 'ready';
  } catch (error) {
    if (!isMounted || requestId !== loadRequestId) {
      return;
    }

    state.value = error instanceof HttpStatusCodeError && error.status === 404
      ? 'notFound'
      : 'error';
  }
}

async function saveApplicationAsync(): Promise<void> {
  if (!hasChanges.value || isSaving.value || isDeleting.value) {
    return;
  }

  const redirectUriValues = normalizedRedirectUris();
  saveError.value = null;
  savedMessage.value = null;
  if (!validateRedirectUris(redirectUriValues)) {
    return;
  }

  isSaving.value = true;
  try {
    const scopes = normalizedAllowedScopes();
    await Applications.updateAsync(
      clientId.value,
      redirectUriValues,
      scopes,
      organizationId.value,
    );
    if (isMounted) {
      redirectUris.value = redirectUriValues.map(value => createRedirectUriEntry(value, value));
      allowedScopes.value = [...scopes];
      initialRedirectUris.value = [...redirectUriValues];
      initialAllowedScopes.value = [...scopes];
      savedMessage.value = t('app.applicationManagement.saved');
    }
  } catch (error) {
    if (!isMounted) {
      return;
    }

    if (error instanceof HttpStatusCodeError && error.status === 404) {
      state.value = 'notFound';
    } else {
      saveError.value = t('app.applicationManagement.errors.saveFailed');
    }
  } finally {
    if (isMounted) {
      isSaving.value = false;
    }
  }
}

async function openDeleteDialogAsync(): Promise<void> {
  deleteConfirmation.value = '';
  deleteError.value = null;
  isDeleteDialogOpen.value = true;
  await nextTick();
  requestAnimationFrame(() => deleteConfirmationInput.value?.focus());
}

function updateDeleteDialogOpen(value: boolean): void {
  if (!isDeleting.value) {
    isDeleteDialogOpen.value = value;
  }
}

async function deleteApplicationAsync(): Promise<void> {
  if (!deleteConfirmationMatches.value || isDeleting.value || isSaving.value) {
    return;
  }

  isDeleting.value = true;
  deleteError.value = null;
  try {
    await Applications.deleteAsync(clientId.value, organizationId.value);
    if (isMounted) {
      await router.replace(applicationsListRoute.value);
    }
  } catch (error) {
    if (!isMounted) {
      return;
    }

    if (error instanceof HttpStatusCodeError && error.status === 404) {
      isDeleteDialogOpen.value = false;
      state.value = 'notFound';
    } else {
      deleteError.value = t('app.applicationManagement.errors.deleteFailed');
    }
  } finally {
    if (isMounted) {
      isDeleting.value = false;
    }
  }
}

onMounted(loadApplicationAsync);

watch([clientId, organizationId], loadApplicationAsync);

onBeforeUnmount(() => {
  isMounted = false;
});
</script>

<style scoped lang="css">
.edit-application-page {
  --change-accent: #f59e0b;

  width: min(100%, 960px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.edit-application-header {
  margin-bottom: 20px;
}

.edit-application-title {
  margin: 0;
  color: var(--text-h);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.edit-application-description {
  margin: 7px 0 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.edit-application-page.is-organization {
  --application-context-accent: #a78bfa;
  --application-context-strong: #8b5cf6;
}

.edit-application-page.is-organization .edit-application-title {
  color: color-mix(in srgb, var(--application-context-accent) 70%, var(--text-h));
}

.edit-application-page.is-organization .application-identity,
.edit-application-page.is-organization .settings-card {
  border-color: color-mix(in srgb, var(--application-context-accent) 27%, var(--border));
  background: color-mix(in srgb, var(--application-context-strong) 6%, var(--surface));
}

.details-state {
  display: grid;
  min-height: 250px;
  padding: 32px;
  box-sizing: border-box;
  place-items: center;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
  text-align: center;
}

.state-content {
  display: flex;
  max-width: 420px;
  flex-direction: column;
  align-items: center;
}

.state-icon {
  display: grid;
  width: 54px;
  height: 54px;
  margin-bottom: 15px;
  place-items: center;
  border-radius: 16px;
  color: var(--accent-hover);
  background: var(--accent-bg);
}

.state-icon .material-symbols-outlined {
  font-size: 30px;
}

.state-icon.loading .material-symbols-outlined,
.button-spinner {
  animation: loading-spin 1s linear infinite;
}

.state-title {
  margin: 0;
  color: var(--text-h);
  font-size: 17px;
  font-weight: 700;
  line-height: 1.4;
}

.state-description {
  margin-top: 6px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.state-action {
  width: auto;
  min-width: 96px;
  margin-top: 18px;
  padding: 0 14px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
}

.application-identity {
  display: grid;
  margin-bottom: 14px;
  padding: 16px 18px;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px 20px;
  border: 1px solid var(--border);
  border-radius: 11px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
}

.application-name {
  grid-column: 1 / -1;
  margin: 0;
  color: var(--text-h);
  font-size: 16px;
  font-weight: 700;
}

.application-id-label,
.application-id {
  font-size: 12px;
  line-height: 1.4;
}

.application-id-label {
  color: var(--text-muted);
}

.application-id {
  color: var(--text);
  font-family: var(--mono);
  text-align: right;
}

.configuration-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.settings-card,
.danger-zone {
  padding: 22px;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
}

.redirect-uri-settings {
  transition:
    border-color 180ms ease,
    background-color 180ms ease,
    box-shadow 180ms ease;
}

.edit-application-page .redirect-uri-settings.changed {
  border-color: color-mix(in srgb, var(--change-accent) 48%, var(--border));
  background: color-mix(in srgb, var(--change-accent) 6%, var(--surface));
  box-shadow: var(--shadow-sm), inset 3px 0 0 var(--change-accent);
}

.settings-title,
.danger-zone-title {
  margin: 0;
  color: var(--text-h);
  font-size: 17px;
  font-weight: 700;
  line-height: 1.4;
}

.settings-description,
.danger-zone-description {
  margin: 5px 0 16px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.redirect-uri-label {
  display: block;
  margin-bottom: 7px;
  color: var(--text);
  font-size: 12px;
  font-weight: 700;
}

.redirect-uri-list {
  display: flex;
  margin: 0;
  padding: 0;
  flex-direction: column;
  gap: 8px;
  list-style: none;
}

.redirect-uri-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.redirect-uri-input {
  min-width: 0;
  height: 40px;
  padding: 0 12px;
  box-sizing: border-box;
  flex: 1;
  color: var(--text-h);
  background: var(--surface);
  border: 1.5px solid var(--border-strong);
  border-radius: 7px;
  outline: none;
  font: 13px/1.55 var(--mono);
  transition:
    color 180ms ease,
    border-color 180ms ease,
    background-color 180ms ease,
    box-shadow 180ms ease;
}

.redirect-uri-input:hover:not(:disabled) {
  border-color: var(--text);
}

.redirect-uri-input:focus {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--focus-ring);
}

.redirect-uri-input.has-error {
  border-color: var(--danger);
}

.redirect-uri-row.changed .redirect-uri-input:not(.has-error) {
  color: color-mix(in srgb, var(--change-accent) 78%, var(--text-h));
  border-color: color-mix(in srgb, var(--change-accent) 68%, var(--border));
  background-color: color-mix(in srgb, var(--change-accent) 12%, var(--surface));
  box-shadow: inset 3px 0 0 var(--change-accent);
}

.redirect-uri-row.changed .redirect-uri-input:focus:not(.has-error) {
  box-shadow:
    inset 3px 0 0 var(--change-accent),
    0 0 0 3px color-mix(in srgb, var(--change-accent) 28%, transparent);
}

.redirect-uri-input:disabled {
  cursor: wait;
  opacity: 0.64;
}

.redirect-uri-remove,
.redirect-uri-add {
  width: auto;
  padding: 0 11px;
  grid-auto-flow: column;
  gap: 5px;
  font: inherit;
  font-size: 12px;
  font-weight: 700;
}

.redirect-uri-remove {
  min-width: 40px;
  padding: 0;
  color: var(--text-muted);
}

.redirect-uri-remove:hover:not(:disabled) {
  color: var(--danger);
  border-color: color-mix(in srgb, var(--danger) 50%, var(--border));
  background: color-mix(in srgb, var(--danger) 8%, transparent);
}

.redirect-uri-row.changed .redirect-uri-remove:not(:hover) {
  color: color-mix(in srgb, var(--change-accent) 78%, var(--text-h));
  border-color: color-mix(in srgb, var(--change-accent) 58%, var(--border));
  background: color-mix(in srgb, var(--change-accent) 10%, transparent);
}

.redirect-uri-add {
  margin-top: 10px;
  color: var(--accent-hover);
  border-color: var(--accent-border);
  background: var(--accent-bg);
}

.redirect-uri-add:hover:not(:disabled) {
  color: var(--on-accent);
  border-color: var(--accent);
  background: var(--accent);
}

.redirect-uri-remove:disabled,
.redirect-uri-add:disabled {
  cursor: wait;
  opacity: 0.64;
}

.field-hint,
.field-error,
.save-message,
.save-error,
.delete-error {
  margin-top: 6px;
  font-size: 12px;
  line-height: 1.45;
}

.field-hint {
  color: var(--text-muted);
}

.field-error,
.save-error,
.delete-error {
  color: var(--danger);
}

.scope-list {
  display: grid;
  margin: 0;
  padding: 0;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  list-style: none;
}

.scope-option {
  display: flex;
  min-height: 42px;
  padding: 0 12px;
  align-items: center;
  gap: 9px;
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text);
  background: var(--surface);
  cursor: pointer;
  transition:
    color 180ms ease,
    border-color 180ms ease,
    background-color 180ms ease,
    box-shadow 180ms ease;
}

.scope-option:has(input:checked) {
  color: var(--accent-hover);
  border-color: var(--accent-border);
  background: var(--accent-bg);
}

.scope-option.changed {
  color: color-mix(in srgb, var(--change-accent) 78%, var(--text-h));
  border-color: color-mix(in srgb, var(--change-accent) 68%, var(--border));
  background: color-mix(in srgb, var(--change-accent) 12%, var(--surface));
  box-shadow: inset 3px 0 0 var(--change-accent);
}

.scope-option input {
  width: 17px;
  height: 17px;
  margin: 0;
  accent-color: var(--accent);
}

.scope-option.changed input {
  accent-color: var(--change-accent);
}

.scope-name {
  font-family: var(--mono);
  font-size: 12px;
  font-weight: 700;
}

.configuration-actions {
  display: flex;
  min-height: 40px;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
}

.save-message {
  margin: 0 auto 0 0;
  color: var(--success);
}

.save-error {
  margin: 0 auto 0 0;
}

.form-action {
  width: auto;
  min-width: 96px;
  padding: 0 14px;
  grid-auto-flow: column;
  gap: 6px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
}

.form-action.primary {
  color: var(--on-accent);
  background: var(--accent);
  border-color: var(--accent);
}

.form-action.primary:hover {
  color: var(--on-accent);
  background: var(--accent-hover);
  border-color: var(--accent-hover);
}

.form-action:disabled {
  cursor: not-allowed;
  opacity: 0.64;
}

.configuration-form[aria-busy="true"] .form-action {
  cursor: wait;
}

.danger-zone {
  margin-top: 28px;
  border-color: color-mix(in srgb, var(--danger) 52%, var(--border));
  background: color-mix(in srgb, var(--danger) 6%, var(--surface));
}

.danger-zone-title {
  color: var(--danger);
}

.danger-zone-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
}

.danger-zone-description {
  max-width: 620px;
  margin: 5px 0 0;
}

.delete-button {
  width: auto;
  min-width: 118px;
  padding: 0 14px;
  grid-auto-flow: column;
  gap: 6px;
  color: var(--danger);
  border-color: color-mix(in srgb, var(--danger) 50%, transparent);
  font: inherit;
  font-size: 13px;
  font-weight: 700;
}

.delete-button:hover {
  color: var(--danger);
  background: color-mix(in srgb, var(--danger) 12%, transparent);
  border-color: var(--danger);
}

.delete-dialog-description {
  margin: 0 0 18px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.55;
}

.delete-dialog-description code {
  display: inline;
  padding: 2px 5px;
  font-size: 12px;
}

@keyframes loading-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .edit-application-page {
    padding-top: 8px;
  }

  .application-identity {
    grid-template-columns: 1fr;
  }

  .application-id {
    text-align: left;
  }

  .settings-card,
  .danger-zone {
    padding: 18px 16px;
  }

  .scope-list {
    grid-template-columns: 1fr;
  }

  .configuration-actions,
  .danger-zone-content {
    align-items: stretch;
    flex-direction: column;
  }

  .form-action,
  .delete-button {
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .state-icon.loading .material-symbols-outlined,
  .button-spinner {
    animation: none;
  }

  .redirect-uri-input,
  .redirect-uri-remove,
  .redirect-uri-add,
  .redirect-uri-settings,
  .scope-option {
    transition-duration: 0.01ms;
  }
}
</style>

<template>
  <section
    class="edit-application-page"
    :class="{ 'is-organization': isOrganization }"
    aria-labelledby="edit-application-title"
  >
    <header class="edit-application-header">
      <h1 id="edit-application-title" class="edit-application-title">
        {{ t('app.applicationManagement.editTitle') }}
      </h1>
      <p v-if="application" class="edit-application-description">
        {{ t('app.applicationManagement.editDescription', { name: application.name }) }}
      </p>
    </header>

    <div v-if="state === 'loading'" class="details-state" role="status">
      <div class="state-content">
        <span class="state-icon loading" aria-hidden="true">
          <span class="material-symbols-outlined">progress_activity</span>
        </span>
        <p class="state-title">{{ t('app.applicationManagement.loadingDetails') }}</p>
      </div>
    </div>

    <div v-else-if="state === 'error'" class="details-state" role="alert">
      <div class="state-content">
        <span class="state-icon" aria-hidden="true">
          <span class="material-symbols-outlined">cloud_off</span>
        </span>
        <p class="state-title">{{ t('app.applicationManagement.loadDetailsFailed') }}</p>
        <button type="button" class="app-button state-action" @click="loadApplicationAsync">
          {{ t('app.applicationManagement.retry') }}
        </button>
      </div>
    </div>

    <div v-else-if="state === 'notFound'" class="details-state" role="alert">
      <div class="state-content">
        <span class="state-icon" aria-hidden="true">
          <span class="material-symbols-outlined">search_off</span>
        </span>
        <h2 class="state-title">{{ t('app.applicationManagement.notFoundTitle') }}</h2>
        <p class="state-description">{{ t('app.applicationManagement.notFoundDescription') }}</p>
        <RouterLink class="app-button state-action" :to="applicationsListRoute">
          {{ t('app.applicationManagement.backToApplications') }}
        </RouterLink>
      </div>
    </div>

    <template v-else-if="application">
      <section class="application-identity" :aria-label="application.name">
        <h2 class="application-name">{{ application.name }}</h2>
        <span class="application-id-label">{{ t('app.applicationManagement.clientId') }}</span>
        <span class="application-id">{{ application.id }}</span>
      </section>

      <form class="configuration-form" :aria-busy="isSaving" @submit.prevent="saveApplicationAsync">
        <section
          class="settings-card redirect-uri-settings"
          :class="{ changed: hasRedirectUriChanges }"
          aria-labelledby="redirect-uris-title"
        >
          <h2 id="redirect-uris-title" class="settings-title">
            {{ t('app.applicationManagement.redirectUrisTitle') }}
          </h2>
          <p class="settings-description">
            {{ t('app.applicationManagement.redirectUrisDescription') }}
          </p>
          <div id="redirect-uri-list-label" class="redirect-uri-label">
            {{ t('app.applicationManagement.redirectUrisLabel') }}
          </div>
          <ul class="redirect-uri-list" aria-labelledby="redirect-uri-list-label">
            <li
              v-for="(entry, index) in redirectUris"
              :key="entry.id"
              class="redirect-uri-row"
              :class="{ changed: isRedirectUriChanged(entry) }"
            >
              <input
                ref="redirectUriInputs"
                v-model="entry.value"
                class="redirect-uri-input"
                :class="{ 'has-error': redirectUrisError }"
                :placeholder="t('app.applicationManagement.redirectUrisPlaceholder')"
                :disabled="isSaving || isDeleting"
                :aria-label="t('app.applicationManagement.redirectUriItemLabel', { number: index + 1 })"
                :aria-invalid="redirectUrisError ? 'true' : undefined"
                :aria-describedby="redirectUrisError ? 'redirect-uris-error' : 'redirect-uris-hint'"
                inputmode="url"
                spellcheck="false"
                @input="handleRedirectUriInput"
              />
              <button
                type="button"
                class="app-button redirect-uri-remove"
                :disabled="isSaving || isDeleting"
                :aria-label="t('app.applicationManagement.removeRedirectUriLabel', { number: index + 1 })"
                @click="removeRedirectUri(entry.id)"
              >
                <span class="material-symbols-outlined" aria-hidden="true">delete</span>
              </button>
            </li>
          </ul>
          <button
            type="button"
            class="app-button redirect-uri-add"
            :disabled="redirectUris.length >= 20 || isSaving || isDeleting"
            @click="addRedirectUriAsync"
          >
            <span class="material-symbols-outlined" aria-hidden="true">add</span>
            <span>{{ t('app.applicationManagement.addRedirectUri') }}</span>
          </button>
          <p v-if="redirectUrisError" id="redirect-uris-error" class="field-error" role="alert">
            {{ redirectUrisError }}
          </p>
          <p v-else id="redirect-uris-hint" class="field-hint">
            {{ t('app.applicationManagement.redirectUrisHint') }}
          </p>
        </section>

        <section class="settings-card" aria-labelledby="allowed-scopes-title">
          <h2 id="allowed-scopes-title" class="settings-title">
            {{ t('app.applicationManagement.allowedScopesTitle') }}
          </h2>
          <p class="settings-description">
            {{ t('app.applicationManagement.allowedScopesDescription') }}
          </p>
          <ul class="scope-list">
            <li v-for="scope in availableScopes" :key="scope">
              <label class="scope-option" :class="{ changed: isScopeChanged(scope) }">
                <input
                  v-model="allowedScopes"
                  type="checkbox"
                  :value="scope"
                  :disabled="scope === requiredScope || isSaving || isDeleting"
                  @change="clearSaveFeedback"
                />
                <span class="scope-name">{{ scope }}</span>
              </label>
            </li>
          </ul>
        </section>

        <div class="configuration-actions">
          <p v-if="saveError" class="save-error" role="alert">{{ saveError }}</p>
          <p v-else-if="savedMessage" class="save-message" role="status">{{ savedMessage }}</p>
          <button
            type="submit"
            class="app-button form-action primary"
            :disabled="!hasChanges || isSaving || isDeleting"
          >
            <span v-if="isSaving" class="material-symbols-outlined button-spinner" aria-hidden="true">
              progress_activity
            </span>
            <span>{{ t('app.applicationManagement.saveChanges') }}</span>
          </button>
        </div>
      </form>

      <section class="danger-zone" aria-labelledby="danger-zone-title">
        <div class="danger-zone-content">
          <div>
            <h2 id="danger-zone-title" class="danger-zone-title">
              {{ t('app.applicationManagement.dangerZone') }}
            </h2>
            <p class="danger-zone-description">
              {{ t('app.applicationManagement.dangerZoneDescription') }}
            </p>
          </div>
          <button
            type="button"
            class="app-button delete-button"
            :disabled="isSaving || isDeleting"
            @click="openDeleteDialogAsync"
          >
            <span class="material-symbols-outlined" aria-hidden="true">delete</span>
            <span>{{ t('app.applicationManagement.deleteAction') }}</span>
          </button>
        </div>
      </section>
    </template>

    <Dialog
      :is-open="isDeleteDialogOpen"
      :title="t('app.applicationManagement.deleteDialogTitle')"
      :close-on-backdrop="!isDeleting"
      :close-on-escape="!isDeleting"
      :show-close-button="!isDeleting"
      @update:is-open="updateDeleteDialogOpen"
    >
      <p class="delete-dialog-description">
        <i18n-t keypath="app.applicationManagement.deleteDialogDescription" tag="span">
          <template #name><code>{{ application?.name }}</code></template>
        </i18n-t>
      </p>
      <FloatingInput
        ref="deleteConfirmationInput"
        v-model="deleteConfirmation"
        :label="t('app.applicationManagement.deleteConfirmationLabel')"
        :hint="t('app.applicationManagement.deleteConfirmationHint', { name: application?.name })"
        :disabled="isDeleting"
        autocomplete="off"
        spellcheck="false"
      />
      <p v-if="deleteError" class="delete-error" role="alert">{{ deleteError }}</p>

      <template #footer>
        <button
          type="button"
          class="app-button form-action"
          :disabled="isDeleting"
          @click="updateDeleteDialogOpen(false)"
        >
          {{ t('app.applicationManagement.createCancel') }}
        </button>
        <button
          type="button"
          class="app-button delete-button"
          :disabled="!deleteConfirmationMatches || isDeleting"
          @click="deleteApplicationAsync"
        >
          <span v-if="isDeleting" class="material-symbols-outlined button-spinner" aria-hidden="true">
            progress_activity
          </span>
          <span>{{ t('app.applicationManagement.deleteConfirmAction') }}</span>
        </button>
      </template>
    </Dialog>
  </section>
</template>
