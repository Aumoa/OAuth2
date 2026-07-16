<script setup lang="ts">
interface Props {
  icon: string;
  label: string;
  to?: string;
}

defineProps<Props>();
</script>

<style scoped lang="css">
.sidebar-main-button {
  display: grid;
  grid-template-columns: 24px minmax(0, 1fr);
  gap: 10px;
  align-items: center;
  width: 100%;
  min-height: 44px;
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

.sidebar-main-button:hover:not(:disabled) {
  color: var(--text-h);
  background: var(--surface-muted);
}

.sidebar-main-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.sidebar-main-button:focus-visible {
  outline: 3px solid var(--focus-ring);
  outline-offset: 1px;
}

.sidebar-main-button.router-link-exact-active {
  border-color: var(--accent-border);
  color: var(--accent-hover);
  background: var(--accent-bg);
  box-shadow: inset 3px 0 0 var(--accent);
}

.sidebar-main-button-icon {
  font-size: 22px;
  text-align: center;
}

.sidebar-main-button.router-link-exact-active .sidebar-main-button-icon {
  font-variation-settings: 'FILL' 1;
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
  <RouterLink v-if="to" class="sidebar-main-button" :to="to">
    <span class="material-symbols-outlined sidebar-main-button-icon" aria-hidden="true">
      {{ icon }}
    </span>
    <span class="sidebar-main-button-label">{{ label }}</span>
  </RouterLink>

  <button v-else class="sidebar-main-button" type="button" disabled>
    <span class="material-symbols-outlined sidebar-main-button-icon" aria-hidden="true">
      {{ icon }}
    </span>
    <span class="sidebar-main-button-label">{{ label }}</span>
  </button>
</template>
