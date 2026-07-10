<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, useId, watch } from 'vue';

type DialogSize = 'small' | 'medium' | 'large';
type DialogCloseReason = 'backdrop' | 'button' | 'escape';

const props = withDefaults(defineProps<{
  isOpen: boolean;
  title?: string;
  ariaLabel?: string;
  closeLabel?: string;
  size?: DialogSize;
  closeOnBackdrop?: boolean;
  closeOnEscape?: boolean;
  showCloseButton?: boolean;
}>(), {
  title: undefined,
  ariaLabel: 'Dialog',
  closeLabel: 'Close dialog',
  size: 'medium',
  closeOnBackdrop: true,
  closeOnEscape: true,
  showCloseButton: true,
});

const emit = defineEmits<{
  'update:isOpen': [value: boolean];
  close: [reason: DialogCloseReason];
}>();

const dialogElement = ref<HTMLDialogElement | null>(null);
const titleId = `dialog-title-${useId()}`;
let previouslyFocusedElement: HTMLElement | null = null;

function restoreFocus() {
  previouslyFocusedElement?.focus();
  previouslyFocusedElement = null;
}

async function syncDialogState(isOpen: boolean) {
  await nextTick();

  const dialog = dialogElement.value;

  if (!dialog) {
    return;
  }

  if (isOpen && !dialog.open) {
    previouslyFocusedElement = document.activeElement instanceof HTMLElement
      ? document.activeElement
      : null;
    dialog.showModal();
    return;
  }

  if (!isOpen && dialog.open) {
    dialog.close();
    restoreFocus();
  }
}

function requestClose(reason: DialogCloseReason) {
  emit('update:isOpen', false);
  emit('close', reason);
}

function handleCancel(event: Event) {
  event.preventDefault();

  if (props.closeOnEscape) {
    requestClose('escape');
  }
}

function handleBackdropClick(event: MouseEvent) {
  if (event.target === dialogElement.value && props.closeOnBackdrop) {
    requestClose('backdrop');
  }
}

watch(() => props.isOpen, syncDialogState);

onMounted(() => syncDialogState(props.isOpen));

onBeforeUnmount(() => {
  if (dialogElement.value?.open) {
    dialogElement.value.close();
  }

  restoreFocus();
});
</script>

<template>
  <dialog
    ref="dialogElement"
    class="dialog"
    :aria-labelledby="title ? titleId : undefined"
    :aria-label="title ? undefined : ariaLabel"
    @cancel="handleCancel"
    @click="handleBackdropClick"
  >
    <section class="dialog-panel" :class="`dialog-panel--${size}`">
      <header v-if="title || $slots.header || showCloseButton" class="dialog-header">
        <div class="dialog-header-content">
          <slot name="header">
            <h2 v-if="title" :id="titleId" class="dialog-title">{{ title }}</h2>
          </slot>
        </div>

        <button
          v-if="showCloseButton"
          type="button"
          class="dialog-close-button"
          :aria-label="closeLabel"
          :title="closeLabel"
          @click="requestClose('button')"
        >
          <span class="material-symbols-outlined" aria-hidden="true">close</span>
        </button>
      </header>

      <div class="dialog-body">
        <slot />
      </div>

      <footer v-if="$slots.footer" class="dialog-footer">
        <slot name="footer" />
      </footer>
    </section>
  </dialog>
</template>

<style scoped>
.dialog {
  position: fixed;
  inset: 0;
  width: 100vw;
  height: 100svh;
  max-width: none;
  max-height: none;
  margin: 0;
  padding: 24px;
  box-sizing: border-box;
  border: 0;
  overflow: visible;
  color: inherit;
  background: transparent;
}

.dialog[open] {
  display: grid;
  place-items: center;
}

.dialog::backdrop {
  background: rgba(8, 12, 11, 0.56);
  backdrop-filter: blur(6px);
  animation: dialog-backdrop-enter 160ms ease-out;
}

.dialog-panel {
  display: flex;
  max-height: min(85svh, 760px);
  flex-direction: column;
  overflow: hidden;
  text-align: left;
  color: var(--text);
  background:
    linear-gradient(145deg, var(--accent-bg), transparent 34%),
    var(--oauth-surface, var(--bg));
  border: 1px solid var(--border);
  border-radius: 16px;
  box-shadow: var(--shadow), 0 24px 64px rgba(8, 12, 11, 0.24);
  animation: dialog-panel-enter 180ms cubic-bezier(0, 0, 0.2, 1);
}

.dialog-panel--small {
  width: min(calc(100vw - 32px), 360px);
}

.dialog-panel--medium {
  width: min(calc(100vw - 32px), 520px);
}

.dialog-panel--large {
  width: min(calc(100vw - 32px), 720px);
}

.dialog-header {
  display: flex;
  min-height: 32px;
  align-items: center;
  gap: 16px;
  padding: 12px 16px 12px 24px;
  border-bottom: 1px solid var(--border);
}

.dialog-header-content {
  min-width: 0;
  flex: 1;
}

.dialog-title {
  margin: 0;
  color: var(--text-h);
  font-family: var(--heading);
  font-size: 1.125rem;
  font-weight: 600;
  line-height: 1.35;
}

.dialog-close-button {
  display: inline-grid;
  width: 40px;
  height: 32px;
  flex: 0 0 auto;
  padding: 0;
  place-items: center;
  color: var(--text);
  background: transparent;
  border: 1px solid transparent;
  border-radius: 10px;
  cursor: pointer;
  transition: color 140ms ease, background-color 140ms ease, border-color 140ms ease;
}

.dialog-close-button:hover {
  color: var(--text-h);
  background: var(--accent-bg);
  border-color: var(--accent-border);
}

.dialog-close-button:focus-visible {
  outline: 3px solid var(--accent-border);
  outline-offset: 2px;
}

.dialog-close-button .material-symbols-outlined {
  font-size: 22px;
}

.dialog-body {
  min-height: 0;
  padding: 24px;
  overflow: auto;
  overscroll-behavior: contain;
}

.dialog-footer {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
  padding: 16px 24px;
  background: var(--oauth-surface-muted, var(--code-bg));
  border-top: 1px solid var(--border);
}

@keyframes dialog-panel-enter {
  from {
    opacity: 0;
    transform: translateY(10px) scale(0.98);
  }

  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@keyframes dialog-backdrop-enter {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@media (max-width: 600px) {
  .dialog {
    padding: 12px;
  }

  .dialog-panel {
    max-height: calc(100svh - 24px);
    border-radius: 14px;
  }

  .dialog-header {
    padding-left: 20px;
  }

  .dialog-body,
  .dialog-footer {
    padding-inline: 20px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .dialog::backdrop,
  .dialog-panel {
    animation-duration: 0.01ms;
  }
}
</style>
