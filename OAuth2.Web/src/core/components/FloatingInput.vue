<script setup lang="ts">
import { computed, onBeforeUnmount, ref, useAttrs, useId } from 'vue';

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
const attrs = useAttrs();
const generatedId = `floating-input-${useId()}`;
const errorNotified = ref(false);
let errorNotificationTimer: ReturnType<typeof setTimeout> | undefined;
const inputId = computed(() => props.id ?? generatedId);
const hasError = computed(() => Boolean(props.error) || errorNotified.value);
const errorMessage = computed(() => typeof props.error === 'string' ? props.error : undefined);
const hintId = computed(() => `${inputId.value}-hint`);
const errorId = computed(() => `${inputId.value}-error`);
const describedBy = computed(() => {
  const ids = [
    typeof attrs['aria-describedby'] === 'string' ? attrs['aria-describedby'] : undefined,
    errorMessage.value ? errorId.value : undefined,
    !errorMessage.value && props.hint ? hintId.value : undefined,
  ];

  return ids.filter(Boolean).join(' ') || undefined;
});

function notifyError(durationMs = 1000): void {
  if (errorNotificationTimer !== undefined) {
    clearTimeout(errorNotificationTimer);
  }

  errorNotified.value = true;
  errorNotificationTimer = setTimeout(() => {
    errorNotified.value = false;
    errorNotificationTimer = undefined;
  }, Math.max(0, durationMs));
}

onBeforeUnmount(() => {
  if (errorNotificationTimer !== undefined) {
    clearTimeout(errorNotificationTimer);
  }
});

defineExpose({
  notifyError,
});
</script>

<style scoped>
.floating-group {
  position: relative;
  display: grid;
  width: 100%;
  gap: 6px;
}

.floating-input {
  width: 100%;
  min-width: 0;
  min-height: 52px;
  box-sizing: border-box;
  padding: 15px 12px 11px;
  color: var(--text-h);
  background: var(--surface);
  border: 1.5px solid var(--border-strong);
  border-radius: 6px;
  outline: none;
  font: inherit;
  font-size: 16px;
  line-height: 1.25;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.floating-input:hover:not(:disabled, :read-only) {
  border-color: var(--text);
}

.floating-input:focus,
.floating-input:focus:hover:not(:disabled, :read-only) {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--focus-ring);
}

.floating-input--error {
  border-color: color-mix(in srgb, var(--danger) 55%, var(--surface));
}

.floating-input--error:hover:not(:disabled, :read-only) {
  border-color: color-mix(in srgb, var(--danger) 75%, var(--surface));
}

.floating-input--error:focus,
.floating-input--error:focus:hover:not(:disabled, :read-only) {
  border-color: var(--danger);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--danger) 28%, transparent);
}

.floating-input:is(:autofill, :-webkit-autofill) {
  -webkit-text-fill-color: var(--text-h);
  box-shadow: 0 0 0 1000px var(--surface) inset;
  caret-color: var(--text-h);
}

.floating-input:is(:autofill, :-webkit-autofill):focus,
.floating-input:is(:autofill, :-webkit-autofill):focus:hover:not(:disabled, :read-only) {
  box-shadow:
    0 0 0 1000px var(--surface) inset,
    0 0 0 3px var(--focus-ring);
}

.floating-input--error:is(:autofill, :-webkit-autofill):focus,
.floating-input--error:is(:autofill, :-webkit-autofill):focus:hover:not(:disabled, :read-only) {
  box-shadow:
    0 0 0 1000px var(--surface) inset,
    0 0 0 3px color-mix(in srgb, var(--danger) 28%, transparent);
}

.floating-input:disabled {
  cursor: not-allowed;
  opacity: 0.58;
}

.floating-input:read-only {
  background: var(--surface-muted);
}

.floating-input::placeholder {
  color: var(--text);
  opacity: 0;
  transition: opacity 0.18s ease;
}

.floating-input:focus::placeholder {
  opacity: 1;
}

.floating-label {
  position: absolute;
  top: 26px;
  left: 12px;
  max-width: calc(100% - 24px);
  padding: 0 4px;
  overflow: hidden;
  color: var(--text);
  background: transparent;
  font-size: 16px;
  line-height: 1;
  pointer-events: none;
  text-overflow: ellipsis;
  white-space: nowrap;
  transform: translateY(-50%);
  transform-origin: left center;
  transition:
    top 0.2s ease,
    left 0.2s ease,
    color 0.2s ease,
    background-color 0.2s ease,
    font-size 0.2s ease;
}

.floating-input:focus + .floating-label {
  color: var(--accent);
}

.floating-input:is(
  :focus,
  :not(:placeholder-shown),
  :autofill,
  :-webkit-autofill
) + .floating-label {
  top: 0;
  left: 8px;
  max-width: calc(100% - 16px);
  background: var(--surface);
  font-size: 13px;
}

.floating-label--error,
.floating-input:is(
  :focus,
  :not(:placeholder-shown),
  :autofill,
  :-webkit-autofill
) + .floating-label--error {
  color: var(--danger);
}

.floating-required {
  color: var(--danger);
}

.floating-message {
  margin: 0;
  padding-inline: 4px;
  color: var(--text);
  font-size: 0.78rem;
  line-height: 1.35;
  text-align: left;
}

.floating-message--error {
  color: var(--danger);
}

.floating-group--error {
  animation: shake 0.3s ease;
}

:global(:root.theme-transitioning .floating-input),
:global(:root.theme-transitioning .floating-label),
:global(:root.theme-transitioning .floating-input::placeholder) {
  transition: none;
}

@keyframes shake {
  0%,
  100% {
    transform: translateX(0);
  }

  20%,
  60% {
    transform: translateX(-5px);
  }

  40%,
  80% {
    transform: translateX(5px);
  }
}

@media (prefers-reduced-motion: reduce) {
  .floating-group,
  .floating-input,
  .floating-label,
  .floating-input::placeholder {
    animation-duration: 0.01ms;
    transition-duration: 0.01ms;
  }
}
</style>

<template>
  <div
    class="floating-group"
    :class="{ 'floating-group--error': hasError }"
  >
    <input
      v-bind="attrs"
      :id="inputId"
      v-model="model"
      class="floating-input"
      :class="{ 'floating-input--error': hasError }"
      :type="props.type"
      :name="props.name"
      :autocomplete="props.autocomplete"
      :placeholder="props.placeholder"
      :disabled="props.disabled"
      :readonly="props.readonly"
      :required="props.required"
      :aria-invalid="hasError ? 'true' : undefined"
      :aria-describedby="describedBy"
    />
    <label
      v-if="props.label"
      :for="inputId"
      class="floating-label"
      :class="{ 'floating-label--error': hasError }"
    >
      {{ props.label }}
      <span v-if="props.required" class="floating-required" aria-hidden="true">*</span>
    </label>
    <p
      v-if="errorMessage"
      :id="errorId"
      class="floating-message floating-message--error"
      role="alert"
    >
      {{ errorMessage }}
    </p>
    <p
      v-else-if="props.hint"
      :id="hintId"
      class="floating-message"
    >
      {{ props.hint }}
    </p>
  </div>
</template>
