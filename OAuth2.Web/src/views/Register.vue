<script setup lang="ts">
import { Accounts, RegisterForm } from '../api/accounts.ts';
import Expander from '../components/common/Expander.vue';
import UnauthenticatedFormLayout from '../components/layout/UnauthenticatedFormLayout.vue';
import { HttpStatusCodeError } from '../core/api/HttpStatusCodeError.ts';
import FloatingInput from '../core/components/FloatingInput.vue';
import { computed, ref } from 'vue';

type State = 'id' | 'password' | 'properties';

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
      return '사용할 ID를 입력하세요.';
    case 'password':
      return '사용할 비밀번호를 입력하세요.';
    case 'properties':
      return '이름과 이메일을 입력하세요.';
  }
});

async function continueAsync() {
  if (requesting.value) {
    return;
  }

  switch (state.value) {
    case 'id':
      if (id.value.trim() === '') {
        idInput.value?.notifyError('ID는 비어있을 수 없습니다.');
        return;
      }

      requesting.value = true;
      try {
        const exists = await Accounts.verifyAsync(id.value);
        if (exists) {
          idInput.value?.notifyError('ID가 이미 존재합니다.');
          return;
        }
      } catch {
        idInput.value?.notifyError('ID 중복 확인에 실패했습니다. 잠시 후 다시 시도해 주세요.');
        return;
      } finally {
        requesting.value = false;
      }

      state.value = 'password';
      setTimeout(() => passwordInput.value?.focus(), 100);
      break;
    case 'password':
      if (password.value.trim() === '') {
        passwordInput.value?.notifyError('암호는 비어있을 수 없습니다.');
        return;
      }

      if (password.value !== passwordRepeat.value) {
        passwordRepeatInput.value?.notifyError('암호가 일치하지 않습니다.');
        return;
      }
      
      state.value = 'properties';
      setTimeout(() => fullnameInput.value?.focus(), 100);
      break;
    case 'properties':
      if (fullname.value.trim() === '') {
        fullnameInput.value?.notifyError('전체 이름을 입력하세요.');
        return;
      }

      if (email.value.trim() === '') {
        emailInput.value?.notifyError('이메일을 입력하세요.');
        return;
      }

      const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/i;
      if (!emailPattern.test(email.value)) {
        emailInput.value?.notifyError('올바르지 않은 이메일입니다.');
        return;
      }
      
      requesting.value = true;
      try {
        await Accounts.registerAsync(new RegisterForm(id.value, password.value, fullname.value, email.value));
      } catch (error) {
        if (error instanceof HttpStatusCodeError && error.status === 409) {
          emailInput.value?.notifyError('이미 사용 중인 ID 또는 이메일입니다.');
        } else {
          emailInput.value?.notifyError('계정 등록에 실패했습니다. 잠시 후 다시 시도해 주세요.');
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
  <UnauthenticatedFormLayout title="계정 등록" :description="description">
    <form class="content" @submit.prevent="continueAsync">
      <FloatingInput ref="idInput" :readonly="readonly.id.value" id="id" label="ID" autocomplete="username" v-model="id" />
      <Expander :expand="visibility.password.value">
        <div class="content">
          <FloatingInput ref="passwordInput" type="password" id="password" label="암호" v-model="password" />
          <FloatingInput ref="passwordRepeatInput" type="password" id="passwordRepeat" label="암호 확인" v-model="passwordRepeat" />
        </div>
      </Expander>
      <Expander :expand="visibility.properties.value">
        <div class="content">
          <FloatingInput ref="fullnameInput" type="text" id="fullname" label="전체 이름" v-model="fullname" />
          <FloatingInput ref="emailInput" type="text" id="email" label="이메일" v-model="email" />
        </div>
      </Expander>
      <div class="button-container">
        <button type="submit" class="app-button accent-button" :disabled="requesting">
          계속
        </button>
        <button v-if="visibility.previous.value" type="button" class="app-button" :disabled="requesting" @click="previous">
          뒤로
        </button>
      </div>
    </form>
  </UnauthenticatedFormLayout>
</template>
