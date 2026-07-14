<script setup lang="ts">
const props = defineProps<{
  title?: string;
  description?: string;
}>();
</script>

<style lang="css">
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
  min-width: 0;
}

.content-container .title {
  margin: 0;
  font-size: 1.45rem;
  font-weight: 650;
  line-height: 1.25;
  text-align: justify;
}

.content-container .description {
  font-size: 0.9rem;
}

.header-container {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.header-container p {
  text-align: left;
}

@media (max-width: 640px) {
  content-container {
    grid-template-columns: 1fr;
  }
}
</style>

<template>
  <div class="content-container">
    <div v-if="title != ''" class="header-container">
      <h2 class="title">{{ props.title }}</h2>
      <p class="description">{{ props.description }}</p>
    </div>
    <slot />
  </div>
</template>