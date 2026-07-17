<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import FloatingInput from '../core/components/FloatingInput.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';
import { useOrganizationsStore } from '../stores/organizations.ts';

const { t } = useI18n();
const router = useRouter();
const organizationsStore = useOrganizationsStore();
const organizationId = ref('');
const organizationName = ref('');
const isCreating = ref(false);
const createError = ref<string | null>(null);
const organizationIdInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const organizationNameInput = ref<InstanceType<typeof FloatingInput> | null>(null);
let isMounted = true;

async function createOrganizationAsync(): Promise<void> {
  if (isCreating.value) {
    return;
  }

  const normalizedId = organizationId.value.trim();
  const normalizedName = organizationName.value.trim();
  createError.value = null;

  if (normalizedId.length === 0) {
    organizationIdInput.value?.notifyError(t('app.organizationManagement.errors.idRequired'));
    return;
  }

  if (normalizedId.length > 64) {
    organizationIdInput.value?.notifyError(t('app.organizationManagement.errors.idTooLong'));
    return;
  }

  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(normalizedId) || normalizedId === 'new') {
    organizationIdInput.value?.notifyError(t('app.organizationManagement.errors.idInvalid'));
    return;
  }

  if (normalizedName.length === 0) {
    organizationNameInput.value?.notifyError(t('app.organizationManagement.errors.nameRequired'));
    return;
  }

  if (normalizedName.length > 128) {
    organizationNameInput.value?.notifyError(t('app.organizationManagement.errors.nameTooLong'));
    return;
  }

  isCreating.value = true;
  try {
    const organization = await organizationsStore.createAsync(normalizedId, normalizedName);
    if (isMounted) {
      await router.replace({
        name: 'organization-management',
        params: { organizationId: organization.id },
      });
    }
  } catch (error) {
    if (!isMounted) {
      return;
    }

    if (error instanceof HttpStatusCodeError && error.status === 409) {
      organizationIdInput.value?.notifyError(t('app.organizationManagement.errors.conflict'));
    } else {
      createError.value = t('app.organizationManagement.errors.createFailed');
    }
  } finally {
    if (isMounted) {
      isCreating.value = false;
    }
  }
}

async function cancelAsync(): Promise<void> {
  if (!isCreating.value) {
    await router.replace({ name: 'applications' });
  }
}

onMounted(() => {
  requestAnimationFrame(() => organizationIdInput.value?.focus());
});

onBeforeUnmount(() => {
  isMounted = false;
});
</script>

<style scoped lang="css">
.create-organization-page {
  --organization-accent: #a78bfa;
  --organization-accent-strong: #8b5cf6;

  width: min(100%, 960px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.create-organization-header {
  margin-bottom: 20px;
}

.create-organization-title {
  margin: 0;
  color: color-mix(in srgb, var(--organization-accent) 72%, var(--text-h));
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.create-organization-description {
  max-width: 640px;
  margin: 7px 0 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.create-organization-form {
  display: flex;
  width: min(100%, 680px);
  padding: 24px;
  box-sizing: border-box;
  flex-direction: column;
  gap: 16px;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 34%, var(--border));
  border-radius: 12px;
  background: color-mix(in srgb, var(--organization-accent-strong) 7%, var(--surface));
  box-shadow: var(--shadow-sm);
}

.create-organization-error {
  margin: 0;
  color: var(--danger);
  font-size: 13px;
  line-height: 1.45;
}

.create-organization-actions {
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
  color: white;
  border-color: var(--organization-accent-strong);
  background: var(--organization-accent-strong);
}

.form-action.primary:hover {
  border-color: color-mix(in srgb, var(--organization-accent-strong) 82%, white);
  background: color-mix(in srgb, var(--organization-accent-strong) 82%, white);
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
  .create-organization-page {
    padding-top: 8px;
  }

  .create-organization-form {
    padding: 20px 16px;
  }

  .create-organization-actions {
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
  <section class="create-organization-page" aria-labelledby="create-organization-title">
    <header class="create-organization-header">
      <h1 id="create-organization-title" class="create-organization-title">
        {{ t('app.organizationManagement.createTitle') }}
      </h1>
      <p class="create-organization-description">
        {{ t('app.organizationManagement.createDescription') }}
      </p>
    </header>

    <form
      class="create-organization-form"
      :aria-busy="isCreating"
      @submit.prevent="createOrganizationAsync"
    >
      <FloatingInput
        id="organization-id"
        ref="organizationIdInput"
        v-model="organizationId"
        :label="t('app.organizationManagement.id')"
        :hint="t('app.organizationManagement.idHint')"
        :maxlength="64"
        :disabled="isCreating"
        autocomplete="off"
        autocapitalize="none"
        required
        spellcheck="false"
      />
      <FloatingInput
        id="organization-name"
        ref="organizationNameInput"
        v-model="organizationName"
        :label="t('app.organizationManagement.name')"
        :maxlength="128"
        :disabled="isCreating"
        autocomplete="off"
        required
      />

      <p v-if="createError" class="create-organization-error" role="alert">
        {{ createError }}
      </p>

      <div class="create-organization-actions">
        <button
          type="button"
          class="app-button form-action"
          :disabled="isCreating"
          @click="cancelAsync"
        >
          {{ t('app.organizationManagement.createCancel') }}
        </button>
        <button type="submit" class="app-button form-action primary" :disabled="isCreating">
          <span v-if="isCreating" class="material-symbols-outlined button-spinner" aria-hidden="true">
            progress_activity
          </span>
          <span>{{ t('app.organizationManagement.createSubmit') }}</span>
        </button>
      </div>
    </form>
  </section>
</template>
