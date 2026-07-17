<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter, type RouteLocationRaw } from 'vue-router';
import { Applications, type OAuthApplicationType } from '../api/applications.ts';
import FloatingInput from '../core/components/FloatingInput.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const clientId = ref('');
const applicationName = ref('');
const applicationType = ref<OAuthApplicationType>('web');
const applicationTypes: readonly OAuthApplicationType[] = ['web', 'android', 'ios', 'desktop'];
const isCreating = ref(false);
const createError = ref<string | null>(null);
const clientIdInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const applicationNameInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const organizationId = computed(() => {
  const value = route.params.organizationId;
  if (value === undefined) {
    return undefined;
  }

  return Array.isArray(value) ? value.join('/') : value;
});
const isOrganization = computed(() => organizationId.value !== undefined);
const createDescription = computed(() => isOrganization.value
  ? t('app.applicationManagement.organizationCreateDescription', {
    organization: organizationId.value,
  })
  : t('app.applicationManagement.createDescription'));
const applicationsListRoute = computed<RouteLocationRaw>(() => organizationId.value === undefined
  ? { name: 'applications-personal' }
  : {
    name: 'applications-organization',
    params: { organizationId: organizationId.value },
  });
let isMounted = true;

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
    await Applications.createAsync(
      normalizedClientId,
      normalizedName,
      applicationType.value,
      organizationId.value,
    );
    if (isMounted) {
      await router.replace(applicationsListRoute.value);
    }
  } catch (error) {
    if (!isMounted) {
      return;
    }

    if (error instanceof HttpStatusCodeError && error.status === 409) {
      clientIdInput.value?.notifyError(t('app.applicationManagement.errors.conflict'));
    } else {
      createError.value = t('app.applicationManagement.errors.createFailed');
    }
  } finally {
    if (isMounted) {
      isCreating.value = false;
    }
  }
}

async function cancelAsync(): Promise<void> {
  if (!isCreating.value) {
    await router.replace(applicationsListRoute.value);
  }
}

onMounted(() => {
  requestAnimationFrame(() => clientIdInput.value?.focus());
});

onBeforeUnmount(() => {
  isMounted = false;
});
</script>

<style scoped lang="css">
.create-application-page {
  width: min(100%, 960px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.create-application-header {
  margin-bottom: 20px;
}

.create-application-title {
  margin: 0;
  color: var(--text-h);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.create-application-description {
  max-width: 640px;
  margin: 7px 0 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.create-application-page.is-organization {
  --application-context-accent: #a78bfa;
  --application-context-strong: #8b5cf6;
}

.create-application-page.is-organization .create-application-title {
  color: color-mix(in srgb, var(--application-context-accent) 70%, var(--text-h));
}

.create-application-page.is-organization .create-application-form {
  border-color: color-mix(in srgb, var(--application-context-accent) 30%, var(--border));
  background: color-mix(in srgb, var(--application-context-strong) 7%, var(--surface));
}

.create-application-form {
  display: flex;
  width: min(100%, 680px);
  padding: 24px;
  box-sizing: border-box;
  flex-direction: column;
  gap: 16px;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
}

.create-application-error {
  margin: 0;
  color: var(--danger);
  font-size: 13px;
  line-height: 1.45;
}

.application-type-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.application-type-label {
  color: var(--text);
  font-size: 13px;
  font-weight: 700;
}

.application-type-select {
  width: 100%;
  min-height: 44px;
  padding: 0 12px;
  box-sizing: border-box;
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text);
  background: var(--surface);
  font: inherit;
  font-size: 14px;
}

.application-type-select:focus-visible {
  border-color: var(--accent);
  outline: 3px solid var(--focus-ring);
  outline-offset: 1px;
}

.application-type-select:disabled {
  cursor: wait;
  opacity: 0.64;
}

.application-type-hint {
  margin: 0;
  color: var(--text-muted);
  font-size: 12px;
  line-height: 1.45;
}

.create-application-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 6px;
}

.form-action {
  width: auto;
  min-width: 84px;
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
  cursor: wait;
  opacity: 0.64;
}

.button-spinner {
  animation: loading-spin 1s linear infinite;
}

@keyframes loading-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .create-application-page {
    padding-top: 8px;
  }

  .create-application-form {
    padding: 20px 16px;
  }

  .create-application-actions {
    flex-direction: column-reverse;
  }

  .form-action {
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .button-spinner {
    animation: none;
  }
}
</style>

<template>
  <section
    class="create-application-page"
    :class="{ 'is-organization': isOrganization }"
    aria-labelledby="create-application-title"
  >
    <header class="create-application-header">
      <h1 id="create-application-title" class="create-application-title">
        {{ t('app.applicationManagement.createTitle') }}
      </h1>
      <p class="create-application-description">
        {{ createDescription }}
      </p>
    </header>

    <form
      class="create-application-form"
      :aria-busy="isCreating"
      @submit.prevent="createApplicationAsync"
    >
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

      <div class="application-type-field">
        <label class="application-type-label" for="application-type">
          {{ t('app.applicationManagement.applicationType') }}
        </label>
        <select
          id="application-type"
          v-model="applicationType"
          class="application-type-select"
          :disabled="isCreating"
        >
          <option v-for="type in applicationTypes" :key="type" :value="type">
            {{ t(`app.applicationManagement.applicationTypes.${type}`) }}
          </option>
        </select>
        <p class="application-type-hint">
          {{ t(`app.applicationManagement.applicationTypeDescriptions.${applicationType}`) }}
        </p>
      </div>

      <p v-if="createError" class="create-application-error" role="alert">{{ createError }}</p>

      <div class="create-application-actions">
        <button
          type="button"
          class="app-button form-action"
          :disabled="isCreating"
          @click="cancelAsync"
        >
          {{ t('app.applicationManagement.createCancel') }}
        </button>
        <button type="submit" class="app-button form-action primary" :disabled="isCreating">
          <span v-if="isCreating" class="material-symbols-outlined button-spinner" aria-hidden="true">
            progress_activity
          </span>
          <span>{{ t('app.applicationManagement.createSubmit') }}</span>
        </button>
      </div>
    </form>
  </section>
</template>
