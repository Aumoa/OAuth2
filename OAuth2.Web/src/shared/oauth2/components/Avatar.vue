<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useAuthStore } from '../src/auth';

const props = withDefaults(defineProps<{
  size?: 'small' | 'large';
  alt?: string;
}>(), {
  size: 'small',
  alt: '',
});

const auth = useAuthStore();

const imageFailed = ref(false);
const picture = computed(() => imageFailed.value ? undefined : auth.user?.picture ?? undefined);
const firstChar = computed(() => (
  auth.user?.name?.at(0)
  ?? auth.user?.id?.at(0)
  ?? auth.user?.sub?.at(0)
  ?? '?'
).toUpperCase());

function onImageError(): void {
  imageFailed.value = true;
}

watch(() => auth.user?.picture, () => {
  imageFailed.value = false;
});
</script>

<style scoped lang="css">
.container {
  display: flex;
  align-self: center;
  flex: 0 0 auto;
}

.avatar {
  display: grid;
  box-sizing: border-box;
  flex: 0 0 auto;
  place-items: center;
  border: 1px solid var(--border);
  border-radius: 50%;
  overflow: hidden;
  object-fit: cover;
}

.avatar.fallback {
  color: var(--on-accent);
  background: var(--accent);
  font-weight: 700;
  text-align: center;
  text-transform: uppercase;
}

.avatar.fallback.large {
  font-size: 30px;
}

.avatar.small {
  width: 26px;
  height: 26px;
}

.avatar.large {
  width: 60px;
  height: 60px;
}
</style>

<template>
  <div class="container">
    <img
      v-if="picture"
      :src="picture"
      :alt="props.alt"
      class="avatar"
      :class="props.size"
      @error="onImageError"
    />
    <span v-else class="avatar fallback" :class="props.size" aria-hidden="true">
      {{ firstChar }}
    </span>
  </div>
</template>
