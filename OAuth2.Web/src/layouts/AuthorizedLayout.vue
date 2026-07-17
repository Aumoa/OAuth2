<script setup lang="ts">
import Sidebar from '../components/Sidebar.vue';
import Topbar from '../components/Topbar.vue';

</script>

<style lang="css">
.main-layout {
  --topbar-height: 50px;
  --sidebar-width: 220px;

  display: grid;
  grid-template-areas:
    'topbar topbar'
    'sidebar content';
  grid-template-columns: var(--sidebar-width) minmax(0, 1fr);
  grid-template-rows: var(--topbar-height) minmax(0, 1fr);
  width: 100%;
  height: 100svh;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}

.main-layout-topbar {
  grid-area: topbar;
}

.main-layout-sidebar {
  grid-area: sidebar;
}

.main-layout-content {
  grid-area: content;
  min-width: 0;
  min-height: 0;
  overflow: auto;
  padding: 16px 24px 16px 40px;
}
</style>

<template>
  <div class="main-layout">
    <Topbar class="main-layout-topbar" />
    <Sidebar class="main-layout-sidebar" title="OAuth2" />
    <main class="main-layout-content">
      <RouterView />
    </main>
  </div>
</template>
