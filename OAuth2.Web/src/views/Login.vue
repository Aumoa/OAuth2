<script setup lang="ts">
import { computed, ref } from 'vue';
import { router } from '../router/index.ts';
import UnauthenticatedFormLayout from '../components/layout/UnauthenticatedFormLayout.vue';
import FloatingInput from '../core/components/FloatingInput.vue';
import { Accounts } from '../api/accounts.ts';

type State = 'id' | 'password';

const state = ref<State>('id');
const id = ref<string>('');
const password = ref<string>('');
const idInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const passwordInput = ref<InstanceType<typeof FloatingInput> | null>(null);

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

async function continueAsync() {
  switch (state.value) {
    case 'id':
      if (!id.value.trim()) {
        idInput.value?.notifyError('ID를 입력하세요.');
        return;
      }

      const exist = await Accounts.verifyAsync(id.value);
      if (exist) {
        state.value = 'password';
        setTimeout(() => passwordInput.value?.focus(), 100);
      }
      else {
        idInput.value?.notifyError('일치하는 계정이 존재하지 않습니다.');
      }

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
  <UnauthenticatedFormLayout title="OAuth2에 로그인">
    <form class="content" @submit.prevent="continueAsync">
      <FloatingInput
        ref="idInput"
        :readonly="readonly.id.value"
        id="id"
        label="ID"
        autocomplete="username"
        v-model="id"
        />
      <FloatingInput
        v-if="visibility.password.value"
        ref="passwordInput"
        id="password"
        label="암호"
        type="password"
        autocomplete="current-password"
        v-model="password"
        />
      <div class="button-container">
        <button type="submit" class="app-button accent-button">
          계속
        </button>
        <button v-if="visibility.previous.value" type="button" class="app-button" @click="previous">
          뒤로
        </button>
        <button v-if="visibility.register.value" type="button" class="app-button" @click="register">
          등록
        </button>
      </div>
    </form>
  </UnauthenticatedFormLayout>
</template>
