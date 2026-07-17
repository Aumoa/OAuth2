<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { OrganizationGroups } from '../api/OrganizationGroups.ts';
import FloatingInput from '../core/components/FloatingInput.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const groupId = ref('');
const groupName = ref('');
const isCreating = ref(false);
const createError = ref<string | null>(null);
const groupIdInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const groupNameInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const organizationId = computed(() => {
  const value = route.params.organizationId;
  return Array.isArray(value) ? value.join('/') : value ?? '';
});
let isMounted = true;

async function createGroupAsync(): Promise<void> {
  if (isCreating.value) {
    return;
  }

  const normalizedId = groupId.value.trim();
  const normalizedName = groupName.value.trim();
  createError.value = null;

  if (normalizedId.length === 0) {
    groupIdInput.value?.notifyError(t('app.organizationManagement.groups.errors.idRequired'));
    return;
  }

  if (normalizedId.length > 64) {
    groupIdInput.value?.notifyError(t('app.organizationManagement.groups.errors.idTooLong'));
    return;
  }

  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(normalizedId) || normalizedId === 'new') {
    groupIdInput.value?.notifyError(t('app.organizationManagement.groups.errors.idInvalid'));
    return;
  }

  if (normalizedName.length === 0) {
    groupNameInput.value?.notifyError(t('app.organizationManagement.groups.errors.nameRequired'));
    return;
  }

  if (normalizedName.length > 128) {
    groupNameInput.value?.notifyError(t('app.organizationManagement.groups.errors.nameTooLong'));
    return;
  }

  isCreating.value = true;
  try {
    const created = await OrganizationGroups.createAsync(
      organizationId.value,
      normalizedId,
      normalizedName,
    );
    if (isMounted) {
      await router.replace({
        name: 'organization-group',
        params: {
          organizationId: created.organizationId,
          groupId: created.id,
        },
      });
    }
  } catch (error) {
    if (!isMounted) {
      return;
    }

    if (error instanceof HttpStatusCodeError && error.status === 409) {
      groupIdInput.value?.notifyError(t('app.organizationManagement.groups.errors.conflict'));
    } else if (error instanceof HttpStatusCodeError && error.status === 403) {
      createError.value = t('app.organizationManagement.groups.errors.forbidden');
    } else {
      createError.value = t('app.organizationManagement.groups.errors.createFailed');
    }
  } finally {
    if (isMounted) {
      isCreating.value = false;
    }
  }
}

async function cancelAsync(): Promise<void> {
  if (!isCreating.value) {
    await router.replace({
      name: 'organization-management',
      params: { organizationId: organizationId.value },
    });
  }
}

onMounted(() => {
  requestAnimationFrame(() => groupIdInput.value?.focus());
});

onBeforeUnmount(() => {
  isMounted = false;
});
</script>

<style scoped lang="css">
.create-group-page {
  --organization-accent: #a78bfa;
  --organization-accent-strong: #8b5cf6;

  width: min(100%, 960px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.create-group-header {
  margin-bottom: 20px;
}

.create-group-title {
  margin: 0;
  color: color-mix(in srgb, var(--organization-accent) 72%, var(--text-h));
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.create-group-description {
  max-width: 640px;
  margin: 7px 0 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.create-group-form {
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

.create-group-error {
  margin: 0;
  color: var(--danger);
  font-size: 13px;
  line-height: 1.45;
}

.create-group-actions {
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
  .create-group-page {
    padding-top: 8px;
  }

  .create-group-form {
    padding: 20px 16px;
  }

  .create-group-actions {
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
  <section class="create-group-page" aria-labelledby="create-group-title">
    <header class="create-group-header">
      <h1 id="create-group-title" class="create-group-title">
        {{ t('app.organizationManagement.groups.createTitle') }}
      </h1>
      <p class="create-group-description">
        {{ t('app.organizationManagement.groups.createDescription', { organization: organizationId }) }}
      </p>
    </header>

    <form
      class="create-group-form"
      :aria-busy="isCreating"
      @submit.prevent="createGroupAsync"
    >
      <FloatingInput
        id="organization-group-id"
        ref="groupIdInput"
        v-model="groupId"
        :label="t('app.organizationManagement.groups.id')"
        :hint="t('app.organizationManagement.groups.idHint')"
        :maxlength="64"
        :disabled="isCreating"
        autocomplete="off"
        autocapitalize="none"
        required
        spellcheck="false"
      />
      <FloatingInput
        id="organization-group-name"
        ref="groupNameInput"
        v-model="groupName"
        :label="t('app.organizationManagement.groups.name')"
        :maxlength="128"
        :disabled="isCreating"
        autocomplete="off"
        required
      />

      <p v-if="createError" class="create-group-error" role="alert">
        {{ createError }}
      </p>

      <div class="create-group-actions">
        <button
          type="button"
          class="app-button form-action"
          :disabled="isCreating"
          @click="cancelAsync"
        >
          {{ t('app.organizationManagement.groups.createCancel') }}
        </button>
        <button type="submit" class="app-button form-action primary" :disabled="isCreating">
          <span v-if="isCreating" class="material-symbols-outlined button-spinner" aria-hidden="true">
            progress_activity
          </span>
          <span>{{ t('app.organizationManagement.groups.createSubmit') }}</span>
        </button>
      </div>
    </form>
  </section>
</template>
