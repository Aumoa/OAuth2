<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import { Accounts } from '../api/accounts.ts';
import { router } from '../router/index.ts';
import UnauthenticatedFormLayout from '../components/UnauthorizedForm.vue/index.js';

type VerificationState =
  | 'pending'
  | 'verifying'
  | 'verified'
  | 'failed'
  | 'resending'
  | 'resent';

const { t } = useI18n({ useScope: 'global' });
const route = useRoute();
const state = ref<VerificationState>('pending');
const sub = ref<string | null>(null);

const title = computed(() => {
  switch (state.value) {
    case 'verifying':
      return t('app.verifyEmail.titles.verifying');
    case 'verified':
      return t('app.verifyEmail.titles.verified');
    case 'failed':
      return t('app.verifyEmail.titles.failed');
    default:
      return t('app.verifyEmail.titles.pending');
  }
});

const description = computed(() => {
  switch (state.value) {
    case 'verifying':
      return t('app.verifyEmail.descriptions.verifying');
    case 'verified':
      return t('app.verifyEmail.descriptions.verified');
    case 'failed':
      return t('app.verifyEmail.descriptions.failed');
    case 'resending':
      return t('app.verifyEmail.descriptions.resending');
    case 'resent':
      return t('app.verifyEmail.descriptions.resent');
    default:
      return t('app.verifyEmail.descriptions.pending');
  }
});

onMounted(async () => {
  const querySub = typeof route.query.sub === 'string' ? route.query.sub : null;
  const queryCode = typeof route.query.code === 'string' ? route.query.code : null;

  if (querySub && queryCode) {
    state.value = 'verifying';
    try {
      await Accounts.verifyEmailAsync(querySub, queryCode);
      state.value = 'verified';
    } catch {
      sub.value = querySub;
      state.value = 'failed';
    }
    return;
  }

  sub.value = Accounts.getPendingEmailVerificationSub();
  if (!sub.value) {
    state.value = 'failed';
  }
});

async function resendAsync(): Promise<void> {
  if (!sub.value || state.value === 'resending') {
    return;
  }

  state.value = 'resending';
  try {
    await Accounts.resendEmailVerificationAsync(sub.value);
    state.value = 'resent';
  } catch {
    state.value = 'failed';
  }
}

async function goToLoginAsync(): Promise<void> {
  await router.replace('/login');
}
</script>

<style lang="css">
.verification-actions {
  display: flex;
  justify-content: center;
  gap: 10px;
}

.verification-actions button {
  width: auto;
  min-width: 140px;
  padding: 0 18px;
}

.verification-actions .accent-button {
  color: var(--on-accent);
  background-color: var(--accent);
}

.verification-actions .accent-button:hover {
  background-color: var(--accent-hover);
}
</style>

<template>
  <UnauthenticatedFormLayout :title="title" :description="description">
    <div class="verification-actions">
      <button
        v-if="sub && (state === 'pending' || state === 'resent' || state === 'failed')"
        type="button"
        class="app-button accent-button"
        @click="resendAsync"
      >
        {{ t('app.verifyEmail.actions.resend') }}
      </button>
      <button
        v-if="state === 'verified' || state === 'failed'"
        type="button"
        class="app-button"
        @click="goToLoginAsync"
      >
        {{ t('app.verifyEmail.actions.goToLogin') }}
      </button>
    </div>
  </UnauthenticatedFormLayout>
</template>
