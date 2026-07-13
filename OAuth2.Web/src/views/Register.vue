<script setup lang="ts">
import { Accounts, RegisterForm } from '../api/accounts.ts';
import Expander from '../core/components/Expander.vue';
import UnauthenticatedFormLayout from '../components/layout/UnauthenticatedFormLayout.vue';
import { HttpStatusCodeError } from '../core/api/HttpStatusCodeError.ts';
import FloatingInput from '../core/components/FloatingInput.vue';
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';

type State = 'id' | 'password' | 'properties';

const { t } = useI18n({ useScope: 'global' });
const state = ref<State>('id');
const id = ref<string>('');
const password = ref<string>('');
const passwordRepeat = ref<string>('');
const fullname = ref<string>('');
const email = ref<string>('');
const requesting = ref(false);

const idInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const passwordInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const passwordRepeatInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const fullnameInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const emailInput = ref<InstanceType<typeof FloatingInput> | null>(null);

const visibility = {
  previous: computed(() => state.value !== 'id'),
  password: computed(() => state.value === 'password'),
  properties: computed(() => state.value === 'properties')
};

const readonly = {
  id: computed(() => state.value !== 'id')
};

const description = computed(() => {
  switch (state.value) {
    case 'id':
      return t('app.register.descriptions.id');
    case 'password':
      return t('app.register.descriptions.password');
    case 'properties':
      return t('app.register.descriptions.properties');
  }
});

async function continueAsync() {
  if (requesting.value) {
    return;
  }

  switch (state.value) {
    case 'id':
      if (id.value.trim() === '') {
        idInput.value?.notifyError(t('app.register.errors.idRequired'));
        return;
      }

      requesting.value = true;
      try {
        const exists = await Accounts.verifyAsync(id.value);
        if (exists) {
          idInput.value?.notifyError(t('app.register.errors.idExists'));
          return;
        }
      } catch {
        idInput.value?.notifyError(t('app.register.errors.idCheckFailed'));
        return;
      } finally {
        requesting.value = false;
      }

      state.value = 'password';
      setTimeout(() => passwordInput.value?.focus(), 100);
      break;
    case 'password':
      if (password.value.trim() === '') {
        passwordInput.value?.notifyError(t('app.register.errors.passwordRequired'));
        return;
      }

      if (password.value !== passwordRepeat.value) {
        passwordRepeatInput.value?.notifyError(t('app.register.errors.passwordMismatch'));
        return;
      }
      
      state.value = 'properties';
      setTimeout(() => fullnameInput.value?.focus(), 100);
      break;
    case 'properties':
      if (fullname.value.trim() === '') {
        fullnameInput.value?.notifyError(t('app.register.errors.fullNameRequired'));
        return;
      }

      if (email.value.trim() === '') {
        emailInput.value?.notifyError(t('app.register.errors.emailRequired'));
        return;
      }

      const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/i;
      if (!emailPattern.test(email.value)) {
        emailInput.value?.notifyError(t('app.register.errors.invalidEmail'));
        return;
      }
      
      requesting.value = true;
      try {
        await Accounts.registerAsync(new RegisterForm(id.value, password.value, fullname.value, email.value));
      } catch (error) {
        if (error instanceof HttpStatusCodeError && error.status === 409) {
          emailInput.value?.notifyError(t('app.register.errors.conflict'));
        } else {
          emailInput.value?.notifyError(t('app.register.errors.failed'));
        }
      } finally {
        requesting.value = false;
      }
      break;
  }
}

function previous() {
  switch (state.value) {
    case 'password':
      password.value = '';
      passwordRepeat.value = '';
      state.value = 'id';
      break;
    case 'properties':
      state.value = 'password';
      setTimeout(() => passwordInput.value?.focus(), 100);
      break;
  }
}
</script>

<style lang="css">
.content {
  display: flex;
  flex-direction: column;
  gap: 10px;
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

.input-container {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
}

.error-text {
  color: transparent;
  font-size: 12px;
  transition: color 0.5s ease;
  text-align: left;
  margin-top: 0px;
}

.error-text--error {
  color: var(--danger);
  transition: color 0.2s ease;
}
</style>

<template>
  <UnauthenticatedFormLayout :title="t('app.register.title')" :description="description">
    <form class="content" @submit.prevent="continueAsync">
      <FloatingInput ref="idInput" :readonly="readonly.id.value" id="id" :label="t('app.common.fields.id')" autocomplete="username" v-model="id" />
      <Expander :expand="visibility.password.value">
        <div class="content">
          <FloatingInput ref="passwordInput" type="password" id="password" :label="t('app.common.fields.password')" v-model="password" />
          <FloatingInput ref="passwordRepeatInput" type="password" id="passwordRepeat" :label="t('app.common.fields.passwordConfirmation')" v-model="passwordRepeat" />
        </div>
      </Expander>
      <Expander :expand="visibility.properties.value">
        <div class="content">
          <FloatingInput ref="fullnameInput" type="text" id="fullname" :label="t('app.common.fields.fullName')" v-model="fullname" />
          <FloatingInput ref="emailInput" type="text" id="email" :label="t('app.common.fields.email')" v-model="email" />
        </div>
      </Expander>
      <div class="button-container">
        <button type="submit" class="app-button accent-button" :disabled="requesting">
          {{ t('app.common.actions.continue') }}
        </button>
        <button v-if="visibility.previous.value" type="button" class="app-button" :disabled="requesting" @click="previous">
          {{ t('app.common.actions.back') }}
        </button>
      </div>
    </form>
  </UnauthenticatedFormLayout>
</template>
