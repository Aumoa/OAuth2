<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import { router } from '../router/index.ts';
import UnauthorizedForm from '../components/UnauthorizedForm.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';
import FloatingInput from '../core/components/FloatingInput.vue';
import { Accounts, type AuthorizationRequest } from '../api/accounts.ts';
import Expander from '../core/components/Expander.vue';
import { Sessions, type RememberedAccount } from '../api/Sessions.ts';

type State = 'loading' | 'invalid' | 'accounts' | 'id' | 'password';

const { t } = useI18n({ useScope: 'global' });
const route = useRoute();
const state = ref<State>('loading');
const id = ref<string>('');
const password = ref<string>('');
const requesting = ref(false);
const rememberedAccounts = ref<RememberedAccount[]>([]);
const passwordPreviousState = ref<'accounts' | 'id'>('id');
const errorMessage = ref<string | null>(null);
const failedPictures = ref<Set<string>>(new Set());
const idInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const passwordInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const consentGranted = ref(false);
const clientName = ref<string | null>(null);

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
    prompt: queryValue('prompt'),
    codeChallenge,
    codeChallengeMethod,
  };
});

const requiresOfflineAccessConsent = computed(() => (
  authorization.value?.scope.split(/\s+/).includes('offline_access') === true
));
const loginTitle = computed(() => clientName.value === null
  ? t('app.login.loadingTitle')
  : t('app.login.title', { client: clientName.value }));

const visibility = {
  password: computed(() => state.value === 'password'),
  idError: computed(() => state.value === 'id'),
  previous: computed(() => state.value === 'password'
    || (state.value === 'id' && rememberedAccounts.value.length > 0)),
  register: computed(() => state.value === 'id'),
};

const readonly = {
  id: computed(() => state.value === 'password'),
};

function previous() {
  switch (state.value) {
    case 'password':
      state.value = passwordPreviousState.value;
      password.value = '';
      break;
    case 'id':
      state.value = 'accounts';
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

function useAnotherAccount(): void {
  errorMessage.value = null;
  id.value = '';
  password.value = '';
  state.value = 'id';
  setTimeout(() => idInput.value?.focus(), 100);
}

function requirePassword(account: RememberedAccount): void {
  errorMessage.value = null;
  id.value = account.id;
  password.value = '';
  passwordPreviousState.value = 'accounts';
  state.value = 'password';
  setTimeout(() => passwordInput.value?.focus(), 100);
}

function accountInitial(account: RememberedAccount): string {
  return (account.name?.at(0) ?? account.id.at(0) ?? '?').toUpperCase();
}

function pictureFailed(accountKey: string): boolean {
  return failedPictures.value.has(accountKey);
}

function onPictureError(accountKey: string): void {
  failedPictures.value = new Set([...failedPictures.value, accountKey]);
}

async function continueWithRememberedAccountAsync(
  account: RememberedAccount,
): Promise<void> {
  if (requesting.value
    || authorization.value === null
    || (requiresOfflineAccessConsent.value && !consentGranted.value)) {
    return;
  }

  if (!account.canSignIn) {
    requirePassword(account);
    return;
  }

  requesting.value = true;
  errorMessage.value = null;
  try {
    await Sessions.continueWithRememberedAccountAsync(
      account.accountKey,
      authorization.value,
      consentGranted.value,
    );
  } catch (error) {
    if (error instanceof HttpStatusCodeError
      && (error.status === 401 || error.status === 404)) {
      account.canSignIn = false;
      requirePassword(account);
    } else {
      errorMessage.value = t('app.login.errors.rememberedSignInFailed');
    }
  } finally {
    requesting.value = false;
  }
}

async function removeRememberedAccountAsync(
  account: RememberedAccount,
): Promise<void> {
  if (requesting.value
    || !window.confirm(t('app.login.confirmRemoveAccount', { account: account.id }))) {
    return;
  }

  requesting.value = true;
  errorMessage.value = null;
  try {
    await Sessions.deleteRememberedAccountAsync(account.accountKey);
    rememberedAccounts.value = rememberedAccounts.value.filter(
      item => item.accountKey !== account.accountKey,
    );
    if (rememberedAccounts.value.length === 0) {
      state.value = 'id';
    }
  } catch {
    errorMessage.value = t('app.login.errors.removeAccountFailed');
  } finally {
    requesting.value = false;
  }
}

async function continueAsync() {
  if (requesting.value
    || authorization.value === null
    || (requiresOfflineAccessConsent.value && !consentGranted.value)) {
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
      passwordPreviousState.value = 'id';
      setTimeout(() => passwordInput.value?.focus(), 100);
      break;
    case 'password':
      if (!password.value.trim()) {
        passwordInput.value?.notifyError(t('app.login.errors.passwordRequired'));
        return;
      }

      requesting.value = true;
      try {
        await Accounts.loginAsync(
          id.value,
          password.value,
          authorization.value,
          consentGranted.value,
        );
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

onMounted(async () => {
  const currentAuthorization = authorization.value;
  if (currentAuthorization === null) {
    window.location.replace('/api/v1/auth/login');
    return;
  }

  try {
    const validation = await Accounts.validateAuthorizationAsync(currentAuthorization);
    if (!validation.isValid
      || validation.normalizedScope !== currentAuthorization.scope
      || !validation.clientName?.trim()) {
      throw new Error(validation.error ?? 'Authorization request is invalid.');
    }

    clientName.value = validation.clientName;
  } catch {
    errorMessage.value = t('app.login.errors.authorizationValidationFailed');
    state.value = 'invalid';
    return;
  }

  try {
    rememberedAccounts.value = await Sessions.getRememberedAccountsAsync();
    state.value = rememberedAccounts.value.length > 0 ? 'accounts' : 'id';
  } catch {
    errorMessage.value = t('app.login.errors.accountListFailed');
    state.value = 'id';
  }
});
</script>

<style scoped lang="css">
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

.status-message,
.error-message {
  font-size: 0.82rem;
  text-align: left;
}

.offline-consent {
  display: flex;
  margin-bottom: 14px;
  padding: 12px 14px;
  gap: 10px;
  border: 1px solid color-mix(in srgb, var(--accent) 42%, var(--border));
  border-radius: 9px;
  color: var(--text);
  background: color-mix(in srgb, var(--accent-bg) 38%, var(--surface));
  font-size: 0.82rem;
  line-height: 1.45;
  cursor: pointer;
}

.offline-consent input {
  width: 16px;
  height: 16px;
  margin: 2px 0 0;
  flex: 0 0 auto;
  accent-color: var(--accent);
}

.offline-consent strong {
  display: block;
  margin-bottom: 2px;
  color: var(--text-h);
}

.error-message {
  color: var(--danger);
}

.remembered-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}

.remembered-list {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: color-mix(in srgb, var(--surface) 96%, transparent);
  border: 1px solid var(--border);
  border-radius: 9px;
}

.remembered-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 44px;
  min-width: 0;
  background: transparent;
}

.remembered-row + .remembered-row {
  border-top: 1px solid var(--border);
}

.remembered-account {
  border: 0;
  color: var(--text-h);
  background: transparent;
  cursor: pointer;
  display: flex;
  flex: 1;
  align-items: center;
  min-width: 0;
  padding: 10px 12px;
  gap: 12px;
  font: inherit;
  text-align: left;
  transition: color 0.15s ease, background-color 0.15s ease;
}

.remembered-account:hover {
  background: var(--surface-muted);
}

.remembered-account:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: -2px;
}

.account-avatar {
  display: grid;
  width: 36px;
  height: 36px;
  flex: 0 0 36px;
  box-sizing: border-box;
  place-items: center;
  border: 1px solid var(--border);
  border-radius: 50%;
  overflow: hidden;
  object-fit: cover;
}

.account-avatar.fallback {
  color: var(--on-accent);
  background: var(--accent);
  font-weight: 700;
}

.account-labels {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.account-primary,
.account-secondary {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.account-primary {
  font-size: 15px;
  font-weight: 700;
  line-height: 1.35;
}

.account-secondary {
  color: var(--text-muted);
  font-size: 12px;
  line-height: 1.35;
}

.remove-account {
  display: grid;
  width: 44px;
  height: auto;
  align-self: stretch;
  padding: 0;
  place-items: center;
  border: 0;
  border-left: 1px solid var(--border);
  border-radius: 0;
  color: var(--text-muted);
  background: transparent;
  cursor: pointer;
  transition: color 0.15s ease, background-color 0.15s ease;
}

.remove-account:hover {
  color: var(--danger);
  background: var(--danger-bg);
}

.remove-account:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: -2px;
}

.remove-account .material-symbols-outlined {
  font-size: 20px;
}

.use-another-account {
  width: auto;
  padding: 0 12px;
  align-self: flex-end;
  font-size: 0.8rem;
}

button:disabled {
  cursor: wait;
  opacity: 0.65;
}
</style>

<template>
  <UnauthorizedForm :title="loginTitle">
    <label
      v-if="requiresOfflineAccessConsent && state !== 'loading' && state !== 'invalid'"
      class="offline-consent"
    >
      <input v-model="consentGranted" type="checkbox" :disabled="requesting" />
      <span>
        <strong>{{ t('app.login.offlineConsentTitle', { client: clientName }) }}</strong>
        {{ t('app.login.offlineConsentDescription') }}
      </span>
    </label>
    <p v-if="state === 'loading'" class="status-message" role="status">
      {{ t('app.login.loadingAccounts') }}
    </p>
    <p v-else-if="state === 'invalid'" class="error-message" role="alert">
      {{ errorMessage }}
    </p>
    <div v-else-if="state === 'accounts'" class="remembered-container">
      <p v-if="errorMessage" class="error-message" role="alert">
        {{ errorMessage }}
      </p>
      <div class="remembered-list">
        <div
          v-for="account in rememberedAccounts"
          :key="account.accountKey"
          class="remembered-row"
        >
          <button
            type="button"
            class="remembered-account"
            :disabled="requesting || (requiresOfflineAccessConsent && !consentGranted)"
            @click="continueWithRememberedAccountAsync(account)"
          >
            <img
              v-if="account.picture && !pictureFailed(account.accountKey)"
              :src="account.picture"
              alt=""
              class="account-avatar"
              referrerpolicy="no-referrer"
              @error="onPictureError(account.accountKey)"
            />
            <span v-else class="account-avatar fallback" aria-hidden="true">
              {{ accountInitial(account) }}
            </span>
            <span class="account-labels">
              <span class="account-primary">
                {{ t('app.login.continueWithAccount', { account: account.name || account.id }) }}
              </span>
              <span class="account-secondary">
                {{ account.email || account.id }}
                <template v-if="!account.canSignIn">
                  · {{ t('app.login.passwordRequired') }}
                </template>
              </span>
            </span>
          </button>
          <button
            type="button"
            class="remove-account"
            :disabled="requesting"
            :title="t('app.login.removeAccount')"
            :aria-label="t('app.login.removeAccountLabel', { account: account.id })"
            @click="removeRememberedAccountAsync(account)"
          >
            <span class="material-symbols-outlined" aria-hidden="true">close</span>
          </button>
        </div>
      </div>
      <button
        type="button"
        class="app-button use-another-account"
        :disabled="requesting"
        @click="useAnotherAccount"
      >
        {{ t('app.login.useAnotherAccount') }}
      </button>
    </div>
    <form v-else class="content" @submit.prevent="continueAsync">
      <p v-if="errorMessage" class="error-message" role="alert">
        {{ errorMessage }}
      </p>
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
        <button
          type="submit"
          class="app-button accent-button"
          :disabled="requesting || (requiresOfflineAccessConsent && !consentGranted)"
        >
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
  </UnauthorizedForm>
</template>
