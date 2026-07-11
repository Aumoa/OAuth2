<script setup lang="ts">
import FloatingInput from '../../core/components/FloatingInput.vue';
import { ref } from 'vue';

defineOptions({
  inheritAttrs: false,
});

const props = withDefaults(defineProps<{
  id?: string;
  label?: string;
  type?: string;
  name?: string;
  autocomplete?: string;
  placeholder?: string;
  hint?: string;
  error?: boolean | string;
  disabled?: boolean;
  readonly?: boolean;
  required?: boolean;
}>(), {
  id: undefined,
  label: undefined,
  type: 'text',
  name: undefined,
  autocomplete: undefined,
  placeholder: ' ',
  hint: undefined,
  error: false,
  disabled: false,
  readonly: false,
  required: false,
});

const model = defineModel<string>({ default: '' });
const duration = 1000;
const input = ref<InstanceType<typeof FloatingInput> | null>(null);
const errorText = ref<string>('');
let errorTimer: ReturnType<typeof setTimeout> | undefined;

const visibility = {
  error: ref<boolean>(false)
};

function notifyError(message: string): void {
  input.value?.notifyError(duration);
  errorText.value = message;
  visibility.error.value = true;
  
  if (errorTimer !== undefined) {
    clearTimeout(errorTimer);
  }
}

function hideError() {
  visibility.error.value = false;

  if (errorTimer === undefined) {
    errorTimer = setTimeout(() => {
      errorText.value = '';
      errorTimer = undefined;
    }, duration);
  }
}

defineExpose({
  notifyError
});
</script>

<style lang="css">
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
  height: 14px;
}

.error-text--error {
  color: var(--danger);
  transition: color 0.2s ease;
}
</style>

<template>
  <div class="input-container">
    <FloatingInput
      ref="input"
      :id="id"
      :label="label"
      :type="type"
      :name="name"
      :autocomplete="autocomplete"
      :placeholder="placeholder"
      :hint="hint"
      :disabled="disabled"
      :readonly="readonly"
      :required="required"
      v-model="model"
      @update:model-value="hideError"
      />
    <a v-if="error" class="error-text" :class="{ 'error-text--error': visibility.error.value }">
      {{ errorText }}
    </a>
  </div>
</template>