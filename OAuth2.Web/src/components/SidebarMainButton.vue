<script setup lang="ts">
import { computed, type CSSProperties } from 'vue';

interface Props {
  icon: string;
  label: string;
  to: string;
  indentLevel?: number;
  tone?: 'default' | 'organization';
}

const props = withDefaults(defineProps<Props>(), {
  indentLevel: 0,
  tone: 'default',
});

const emit = defineEmits<{
  focus: [event: FocusEvent];
  blur: [event: FocusEvent];
}>();
const buttonStyle = computed<CSSProperties>(() => ({
  '--sidebar-main-button-indent': `${Math.max(props.indentLevel, 0) * 20}px`,
}));

function onFocus(event: FocusEvent): void {
  if ((event.currentTarget as HTMLElement | null)?.matches(':focus-visible')) {
    emit('focus', event);
  }
}
</script>

<style scoped lang="css">
.sidebar-main-button {
  display: grid;
  grid-template-columns: 24px minmax(0, 1fr);
  gap: 10px;
  align-items: center;
  position: relative;
  z-index: 1;
  width: calc(100% - var(--sidebar-main-button-indent, 0px));
  height: var(--sidebar-main-button-height, 44px);
  margin-left: var(--sidebar-main-button-indent, 0px);
  padding: 0 12px;
  box-sizing: border-box;
  border: 1px solid transparent;
  border-radius: 9px;
  color: var(--text);
  background: transparent;
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  line-height: 1.35;
  text-align: left;
  text-decoration: none;
  cursor: pointer;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease,
    box-shadow 140ms ease;
}

.sidebar-main-button.is-indented {
  font-size: 13px;
}

.sidebar-main-button.is-indented .sidebar-main-button-icon {
  font-size: 20px;
}

.sidebar-main-button:hover {
  color: var(--text-h);
  background: var(--surface-muted);
}

.sidebar-main-button:focus-visible {
  outline: 3px solid var(--focus-ring);
  outline-offset: 1px;
}

.sidebar-main-button.router-link-exact-active {
  color: var(--accent-hover);
  background: transparent;
}

.sidebar-main-button-icon {
  font-size: 22px;
  text-align: center;
}

.sidebar-main-button.router-link-exact-active .sidebar-main-button-icon {
  font-variation-settings: 'FILL' 1;
}

.sidebar-main-button.is-organization {
  color: color-mix(in srgb, #a78bfa 76%, var(--text));
}

.sidebar-main-button.is-organization:hover {
  color: color-mix(in srgb, #c4b5fd 82%, var(--text-h));
  background: color-mix(in srgb, #8b5cf6 9%, var(--surface-muted));
}

.sidebar-main-button.is-organization.router-link-exact-active {
  color: color-mix(in srgb, #a78bfa 84%, var(--text-h));
}

.sidebar-main-button-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (prefers-reduced-motion: reduce) {
  .sidebar-main-button {
    transition-duration: 0.01ms;
  }
}
</style>

<template>
  <RouterLink
    class="sidebar-main-button"
    :class="{
      'is-indented': indentLevel > 0,
      'is-organization': tone === 'organization',
    }"
    :style="buttonStyle"
    :to="to"
    @focus="onFocus"
    @blur="emit('blur', $event)"
  >
    <span class="material-symbols-outlined sidebar-main-button-icon" aria-hidden="true">
      {{ icon }}
    </span>
    <span class="sidebar-main-button-label">{{ label }}</span>
  </RouterLink>
</template>
