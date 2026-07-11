<script setup lang="ts">
import { ref, computed } from 'vue';

let shakeTimer: ReturnType<typeof setTimeout> | undefined;
const shakeClass = ref<boolean>(true);
const props = defineProps<{
  message?: string | null
}>();

function shake() {
  if (shakeTimer != null) {
    clearTimeout(shakeTimer);
    shakeClass.value = true;
  }

  shakeTimer = setTimeout(() => {
    shakeClass.value = false;
  }, 400);
}

const messages = computed(() => {
  return props.message?.split('\\n') ?? [];
})
</script>

<style lang="css">
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
}

h4 {
  color: var(--danger);
}

@keyframes shake {
  0% {
    transform: translateX(0);
  }

  20% {
    transform: translateX(-5px);
  }

  40% {
    transform: translateX(5px);
  }

  60% {
    transform: translateX(-3px);
  }

  80% {
    transform: translateX(3px);
  }

  100% {
    transform: translateX(0);
  }
}

.material-symbols-outlined.error-icon {
  color: var(--danger);
  font-size: 256px;
  line-height: 1;
}

.material-symbols-outlined.error-icon.shake {
  animation: shake 0.4s;
}

.error-message {
  font-size: 1.2em;
}
</style>

<template>
  <div class="error-container">
    <p class="material-symbols-outlined error-icon" :class="{ shake: shakeClass }" @click="shake">error</p>
    <div v-for="message in messages">
      <p class="error-message">{{ message }}</p>
    </div>
  </div>
</template>