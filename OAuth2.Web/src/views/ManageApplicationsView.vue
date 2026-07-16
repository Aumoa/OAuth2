<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Applications, type ApplicationSummary } from '../api/applications.ts';
import Dialog from '../core/components/Dialog.vue';
import FloatingInput from '../core/components/FloatingInput.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

type ViewState = 'loading' | 'ready' | 'error';

const { locale, t } = useI18n();
const state = ref<ViewState>('loading');
const applications = ref<ApplicationSummary[]>([]);
const isCreateDialogOpen = ref(false);
const isCreating = ref(false);
const clientId = ref('');
const applicationName = ref('');
const createError = ref<string | null>(null);
const clientIdInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const applicationNameInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const dateFormatter = computed(() => new Intl.DateTimeFormat(locale.value, {
  dateStyle: 'medium',
}));

function formatCreatedAt(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : dateFormatter.value.format(date);
}

async function loadApplicationsAsync(): Promise<void> {
  state.value = 'loading';

  try {
    applications.value = await Applications.listAsync();
    state.value = 'ready';
  } catch {
    state.value = 'error';
  }
}

async function openCreateDialogAsync(): Promise<void> {
  isCreateDialogOpen.value = true;
  await nextTick();
  requestAnimationFrame(() => clientIdInput.value?.focus());
}

function resetCreateForm(): void {
  clientId.value = '';
  applicationName.value = '';
  createError.value = null;
  clientIdInput.value?.clearError();
  applicationNameInput.value?.clearError();
}

function updateCreateDialogOpen(value: boolean): void {
  if (isCreating.value) {
    return;
  }

  isCreateDialogOpen.value = value;
  if (!value) {
    resetCreateForm();
  }
}

async function createApplicationAsync(): Promise<void> {
  if (isCreating.value) {
    return;
  }

  const normalizedClientId = clientId.value.trim();
  const normalizedName = applicationName.value.trim();
  createError.value = null;

  if (normalizedClientId.length === 0) {
    clientIdInput.value?.notifyError(t('app.applicationManagement.errors.clientIdRequired'));
    return;
  }

  if (normalizedClientId.length > 128) {
    clientIdInput.value?.notifyError(t('app.applicationManagement.errors.clientIdTooLong'));
    return;
  }

  if (normalizedName.length === 0) {
    applicationNameInput.value?.notifyError(t('app.applicationManagement.errors.nameRequired'));
    return;
  }

  if (normalizedName.length > 512) {
    applicationNameInput.value?.notifyError(t('app.applicationManagement.errors.nameTooLong'));
    return;
  }

  isCreating.value = true;
  try {
    const application = await Applications.createAsync(normalizedClientId, normalizedName);
    applications.value = [
      application,
      ...applications.value.filter(existing => existing.id !== application.id),
    ];
    state.value = 'ready';
    isCreateDialogOpen.value = false;
    resetCreateForm();
  } catch (error) {
    if (error instanceof HttpStatusCodeError && error.status === 409) {
      clientIdInput.value?.notifyError(t('app.applicationManagement.errors.conflict'));
    } else {
      createError.value = t('app.applicationManagement.errors.createFailed');
    }
  } finally {
    isCreating.value = false;
  }
}

onMounted(loadApplicationsAsync);
</script>

<style scoped lang="css">
.applications-page {
  width: min(100%, 960px);
  margin: 0 auto;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.applications-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 20px;
}

.applications-heading {
  min-width: 0;
}

.applications-title {
  margin: 0;
  color: var(--text-h);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.applications-description {
  margin-top: 7px;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.create-application-button {
  width: auto;
  height: 40px;
  flex: 0 0 auto;
  padding: 0 14px;
  grid-auto-flow: column;
  gap: 7px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
  white-space: nowrap;
}

.creation-description {
  margin: 0 0 20px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.55;
}

.creation-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.creation-error {
  margin: 0;
  color: var(--danger);
  font-size: 13px;
  line-height: 1.45;
}

.dialog-action {
  width: auto;
  min-width: 84px;
  padding: 0 14px;
  grid-auto-flow: column;
  gap: 6px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
}

.dialog-action.primary {
  color: var(--on-accent);
  background: var(--accent);
  border-color: var(--accent);
}

.dialog-action.primary:hover {
  color: var(--on-accent);
  background: var(--accent-hover);
  border-color: var(--accent-hover);
}

.dialog-action:disabled {
  cursor: wait;
  opacity: 0.64;
}

.button-spinner {
  animation: loading-spin 1s linear infinite;
}

.applications-state {
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
  max-width: 400px;
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

.state-icon.loading .material-symbols-outlined {
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

.retry-button {
  width: auto;
  min-width: 92px;
  height: 38px;
  margin-top: 18px;
  padding: 0 14px;
  grid-auto-flow: column;
  gap: 6px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
}

.application-list {
  display: flex;
  margin: 0;
  padding: 0;
  flex-direction: column;
  gap: 10px;
  list-style: none;
}

.application-card {
  display: grid;
  grid-template-columns: 46px minmax(0, 1fr) auto;
  gap: 14px;
  align-items: center;
  min-height: 76px;
  padding: 14px 18px;
  box-sizing: border-box;
  border: 1px solid var(--border);
  border-radius: 11px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
}

.application-icon {
  display: grid;
  width: 46px;
  height: 46px;
  place-items: center;
  border-radius: 12px;
  color: var(--accent-hover);
  background: var(--accent-bg);
}

.application-icon .material-symbols-outlined {
  font-size: 25px;
}

.application-identity {
  min-width: 0;
}

.application-name,
.application-id {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.application-name {
  margin: 0;
  color: var(--text-h);
  font-size: 15px;
  font-weight: 700;
  line-height: 1.4;
}

.application-id {
  margin-top: 3px;
  color: var(--text-muted);
  font-family: var(--mono);
  font-size: 11px;
  line-height: 1.4;
}

.application-date {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  color: var(--text-muted);
  font-size: 11px;
  line-height: 1.45;
  white-space: nowrap;
}

.application-date time {
  color: var(--text);
  font-size: 12px;
  font-weight: 600;
}

@keyframes loading-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .applications-page {
    padding-top: 8px;
  }

  .applications-header {
    flex-direction: column;
    gap: 14px;
  }

  .create-application-button {
    align-self: stretch;
  }

  .applications-state {
    min-height: 220px;
    padding: 24px 18px;
  }

  .application-card {
    grid-template-columns: 42px minmax(0, 1fr);
    padding: 13px 14px;
  }

  .application-icon {
    width: 42px;
    height: 42px;
  }

  .application-date {
    grid-column: 2;
    align-items: flex-start;
  }
}

@media (prefers-reduced-motion: reduce) {
  .state-icon.loading .material-symbols-outlined,
  .button-spinner {
    animation: none;
  }
}
</style>

<template>
  <section class="applications-page" aria-labelledby="applications-title">
    <header class="applications-header">
      <div class="applications-heading">
        <h1 id="applications-title" class="applications-title">
          {{ t('app.applicationManagement.title') }}
        </h1>
        <p class="applications-description">
          {{ t('app.applicationManagement.description') }}
        </p>
      </div>

      <button
        type="button"
        class="app-button create-application-button"
        aria-haspopup="dialog"
        @click="openCreateDialogAsync"
      >
        <span class="material-symbols-outlined" aria-hidden="true">add</span>
        <span>{{ t('app.applicationManagement.createAction') }}</span>
      </button>
    </header>

    <div v-if="state === 'loading'" class="applications-state" role="status">
      <div class="state-content">
        <span class="state-icon loading" aria-hidden="true">
          <span class="material-symbols-outlined">progress_activity</span>
        </span>
        <p class="state-title">{{ t('app.applicationManagement.loading') }}</p>
      </div>
    </div>

    <div v-else-if="state === 'error'" class="applications-state" role="alert">
      <div class="state-content">
        <span class="state-icon" aria-hidden="true">
          <span class="material-symbols-outlined">cloud_off</span>
        </span>
        <p class="state-title">{{ t('app.applicationManagement.loadFailed') }}</p>
        <button type="button" class="app-button retry-button" @click="loadApplicationsAsync">
          <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
          <span>{{ t('app.applicationManagement.retry') }}</span>
        </button>
      </div>
    </div>

    <div v-else-if="applications.length === 0" class="applications-state" role="status">
      <div class="state-content">
        <span class="state-icon" aria-hidden="true">
          <span class="material-symbols-outlined">apps</span>
        </span>
        <h2 class="state-title">{{ t('app.applicationManagement.emptyTitle') }}</h2>
        <p class="state-description">{{ t('app.applicationManagement.emptyDescription') }}</p>
      </div>
    </div>

    <ul v-else class="application-list">
      <li v-for="application in applications" :key="application.id">
        <article class="application-card">
          <span class="application-icon" aria-hidden="true">
            <span class="material-symbols-outlined">web_asset</span>
          </span>

          <div class="application-identity">
            <h2 class="application-name" :title="application.name">{{ application.name }}</h2>
            <p class="application-id" :title="application.id">
              {{ t('app.applicationManagement.clientId') }}: {{ application.id }}
            </p>
          </div>

          <div class="application-date">
            <span>{{ t('app.applicationManagement.createdAt') }}</span>
            <time :datetime="application.createdAt">
              {{ formatCreatedAt(application.createdAt) }}
            </time>
          </div>
        </article>
      </li>
    </ul>

    <Dialog
      :is-open="isCreateDialogOpen"
      :title="t('app.applicationManagement.createTitle')"
      :close-on-backdrop="!isCreating"
      :close-on-escape="!isCreating"
      :show-close-button="!isCreating"
      @update:is-open="updateCreateDialogOpen"
    >
      <p class="creation-description">
        {{ t('app.applicationManagement.createDescription') }}
      </p>

      <form id="create-application-form" class="creation-form" @submit.prevent="createApplicationAsync">
        <FloatingInput
          id="application-client-id"
          ref="clientIdInput"
          v-model="clientId"
          :label="t('app.applicationManagement.clientId')"
          :maxlength="128"
          :disabled="isCreating"
          autocomplete="off"
          required
          spellcheck="false"
        />
        <FloatingInput
          id="application-name"
          ref="applicationNameInput"
          v-model="applicationName"
          :label="t('app.applicationManagement.name')"
          :maxlength="512"
          :disabled="isCreating"
          autocomplete="off"
          required
        />
        <p v-if="createError" class="creation-error" role="alert">{{ createError }}</p>
      </form>

      <template #footer>
        <button
          type="button"
          class="app-button dialog-action"
          :disabled="isCreating"
          @click="updateCreateDialogOpen(false)"
        >
          {{ t('app.applicationManagement.createCancel') }}
        </button>
        <button
          type="submit"
          form="create-application-form"
          class="app-button dialog-action primary"
          :disabled="isCreating"
        >
          <span v-if="isCreating" class="material-symbols-outlined button-spinner" aria-hidden="true">
            progress_activity
          </span>
          <span>{{ t('app.applicationManagement.createSubmit') }}</span>
        </button>
      </template>
    </Dialog>
  </section>
</template>
