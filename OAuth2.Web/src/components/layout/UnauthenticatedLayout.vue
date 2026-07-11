<script setup lang="ts">
import LanguageSelector from '../../core/components/LanguageSelector.vue';
import ThemeSelector from '../../core/components/ThemeSelector.vue';

const props = defineProps<{
  title: string;
}>();
</script>

<style lang="css">
.control-panel {
  display: flex;
  flex-direction: row;
  align-self: flex-end;
  gap: 10px;
  margin: 5px;
}

.root-panel {
  flex: 1;
  place-items: center;
  flex-direction: column;
  align-content: center;
}

.content-container {
  width: min(760px, 100%);
  box-sizing: border-box;
  padding: 32px;
  border: 1px solid var(--border);
  border-radius: 8px;
  box-shadow: var(--shadow);
  background: color-mix(in srgb, var(--bg) 97%, var(--text-h));
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 32px;
}

.content-container .title {
  margin: 0;
  font-size: 1.45rem;
  font-weight: 650;
  line-height: 1.25;
  text-align: justify;
}

.content-container {
  min-width: 0;
}

@media (max-width: 640px) {
  content-container {
    grid-template-columns: 1fr;
  }
}
</style>

<template>
  <div class="control-panel">
    <LanguageSelector />
    <ThemeSelector />
  </div>
  <div class="root-panel">
    <div class="content-container">
      <h2 class="title">{{ props.title }}</h2>
      <slot />
    </div>
  </div>
</template>