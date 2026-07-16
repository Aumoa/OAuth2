<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { Applications } from '../api/applications.ts';
import FloatingInput from '../core/components/FloatingInput.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

const { t } = useI18n();
const router = useRouter();
const clientId = ref('');
const applicationName = ref('');
const isCreating = ref(false);
const createError = ref<string | null>(null);
const clientIdInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const applicationNameInput = ref<InstanceType<typeof FloatingInput> | null>(null);
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
    await Applications.createAsync(normalizedClientId, normalizedName);
    if (isMounted) {
      await router.replace('/applications');
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
    await router.replace('/applications');
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
  margin: 0 auto;
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
  <section class="create-application-page" aria-labelledby="create-application-title">
    <header class="create-application-header">
      <h1 id="create-application-title" class="create-application-title">
        {{ t('app.applicationManagement.createTitle') }}
      </h1>
      <p class="create-application-description">
        {{ t('app.applicationManagement.createDescription') }}
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
