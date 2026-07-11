<script setup lang="ts">
const props = defineProps<{
  expand: boolean
}>();
</script>

<style lang="css">
.expand {
  display: grid;
  grid-template-rows: 1.0fr;
}

.expand-inner {
  min-height: 0;
}

.expand-enter-active,
.expand-leave-active {
  transition:
    grid-template-rows 0.2s ease,
    opacity 0.2s ease;
}

.expand-enter-from,
.expand-leave-to {
  grid-template-rows: 0fr;
  opacity: 0;
}

.expand-enter-to,
.expand-leave-from {
  grid-template-rows: 1.0fr;
  opacity: 1;
}
</style>

<template>
  <Transition name="expand">
    <div v-if="props.expand" class="expand">
      <div class="expand-inner">
        <slot />
      </div>
    </div>
  </Transition>
</template>