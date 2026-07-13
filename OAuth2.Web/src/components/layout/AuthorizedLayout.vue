<script setup lang="ts">
import Sidebar from '../Sidebar.vue';
import Topbar from '../Topbar.vue';

</script>

<style lang="css">
.main-layout {
  display: flex;
  flex-direction: row;
}
</style>

<template>
  <div class="main-layout">
    <Sidebar title="OAuth2" />
    <Topbar />
  </div>
</template>