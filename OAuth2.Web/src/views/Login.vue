<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import { router } from '../router/index.ts';
import UnauthenticatedFormLayout from '../components/UnauthorizedForm.vue/index.js';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';
import FloatingInput from '../core/components/FloatingInput.vue';
import { Accounts, type AuthorizationRequest } from '../api/accounts.ts';
import Expander from '../core/components/Expander.vue';

type State = 'id' | 'password';

const { t } = useI18n({ useScope: 'global' });
const route = useRoute();
const state = ref<State>('id');
const id = ref<string>('');
const password = ref<string>('');
const requesting = ref(false);
const idInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const passwordInput = ref<InstanceType<typeof FloatingInput> | null>(null);

const authorization = computed<AuthorizationRequest | null>(() => {
  const clientId = queryValue('client_id');
  const redirectUri = queryValue('redirect_uri');
  const responseType = queryValue('response_type');
  const scope = queryValue('scope');
  const authorizationState = queryValue('state');
  const codeChallenge = queryValue('code_challenge');
  const codeChallengeMethod = queryValue('code_challenge_method');

  if (
    !clientId
    || !redirectUri
    || !responseType
    || !scope
    || !authorizationState
    || !codeChallenge
    || !codeChallengeMethod
  ) {
    return null;
  }

  return {
    clientId,
    redirectUri,
    responseType,
    scope,
    state: authorizationState,
    nonce: queryValue('nonce'),
    codeChallenge,
    codeChallengeMethod,
  };
});

const visibility = {
  password: computed(() => state.value === 'password'),
  idError: computed(() => state.value === 'id'),
  previous: computed(() => state.value === 'password'),
  register: computed(() => state.value === 'id')
};

const readonly = {
  id: computed(() => state.value === 'password')
};

function previous() {
  switch (state.value) {
    case 'password':
      state.value = 'id';
      break;
  }
}

function register() {
  switch (state.value) {
    case 'id':
      router.push('/register');
      break;
  }
}

function queryValue(name: string): string | null {
  const value = route.query[name];
  return typeof value === 'string' ? value : null;
}

async function continueAsync() {
  if (requesting.value || authorization.value === null) {
    return;
  }

  switch (state.value) {
    case 'id':
      if (!id.value.trim()) {
        idInput.value?.notifyError(t('app.login.errors.idRequired'));
        return;
      }

      requesting.value = true;
      try {
        const exists = await Accounts.verifyAsync(id.value);
        if (!exists) {
          idInput.value?.notifyError(t('app.login.errors.accountNotFound'));
          return;
        }
      } catch {
        idInput.value?.notifyError(t('app.login.errors.accountLookupFailed'));
        return;
      } finally {
        requesting.value = false;
      }

      state.value = 'password';
      setTimeout(() => passwordInput.value?.focus(), 100);
      break;
    case 'password':
      if (!password.value.trim()) {
        passwordInput.value?.notifyError(t('app.login.errors.passwordRequired'));
        return;
      }

      requesting.value = true;
      try {
        await Accounts.loginAsync(id.value, password.value, authorization.value);
      } catch (error) {
        if (error instanceof HttpStatusCodeError && error.status === 401) {
          passwordInput.value?.notifyError(t('app.login.errors.invalidPassword'));
        } else {
          passwordInput.value?.notifyError(t('app.login.errors.failed'));
        }
      } finally {
        requesting.value = false;
      }
      break;
  }
}

onMounted(() => {
  if (authorization.value === null) {
    window.location.replace('/api/v1/auth/login');
  }
});
</script>

<style lang="css">
.content {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.accent-button {
  background-color: color-mix(in srgb, var(--accent-bg) 80%, var(--accent-hover));
}

.accent-button:hover {
  background-color: color-mix(in srgb, var(--accent-bg) 60%, var(--accent-hover));
}

.button-container {
  margin-top: 20px;
  display: flex;
  flex-direction: row-reverse;
  gap: 10px;
}

.button-container button {
  width: 100px;
  font-size: 0.8rem;
}
</style>

<template>
  <UnauthenticatedFormLayout :title="t('app.login.title')">
    <form class="content" @submit.prevent="continueAsync">
      <FloatingInput
        ref="idInput"
        :readonly="readonly.id.value"
        id="id"
        :label="t('app.common.fields.id')"
        autocomplete="username"
        v-model="id"
        />
      <Expander :expand="visibility.password.value">
        <FloatingInput
          ref="passwordInput"
          id="password"
          :label="t('app.common.fields.password')"
          type="password"
          autocomplete="current-password"
          v-model="password"
          />
      </Expander>
      <div class="button-container">
        <button type="submit" class="app-button accent-button" :disabled="requesting">
          {{ t('app.common.actions.continue') }}
        </button>
        <button v-if="visibility.previous.value" type="button" class="app-button" :disabled="requesting" @click="previous">
          {{ t('app.common.actions.back') }}
        </button>
        <button v-if="visibility.register.value" type="button" class="app-button" :disabled="requesting" @click="register">
          {{ t('app.common.actions.register') }}
        </button>
      </div>
    </form>
  </UnauthenticatedFormLayout>
</template>
