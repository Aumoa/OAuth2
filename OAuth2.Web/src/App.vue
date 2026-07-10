<script setup lang="ts">
import AuthorizedLayout from './components/authorized/AuthorizedLayout.vue';
import UnauthorizedLayout from './components/unauthorized/UnauthorizedLayout.vue';
import UndefinedLayout from './components/undefined/UndefinedLayout.vue';
import Switcher from './core/components/Switcher.vue';
import { ref } from 'vue';
import './core/scripts/theme.ts';

type State = "undefined" | "unauthorized" | "authorized";

const state = ref<State>("undefined");

initializeAsync()
  .catch((error) => {
    console.error(error);
  });

async function initializeAsync() {
  await new Promise<void>((resolve) => setTimeout(resolve, 1000));
  state.value = "unauthorized";
}

function asNumber(state: State): number {
  switch (state) {
    case "undefined":
      return 0;
    case "unauthorized":
      return 1;
    case "authorized":
      return 2;
    default:
      return -1;
  }
}
</script>

<template>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
  <Switcher :index="asNumber(state)">
    <span>
      <UndefinedLayout />
    </span>
    <span>
      <UnauthorizedLayout />
    </span>
    <span>
      <AuthorizedLayout />
    </span>
  </Switcher>
</template>
