<script setup lang="ts">
import LanguageSelector from '../core/components/LanguageSelector.vue';
import ThemeSelector from '../core/components/ThemeSelector.vue';

</script>

<style lang="css">
.top-layout {
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 50px;
  display: flex;
  flex-direction: row;
  background-color: var(--topbar-bg);
  box-sizing: border-box;
  border-bottom: 1px solid var(--border-strong);
}

.title-box {
  width: 220px;
  display: flex;
  align-content: center;
  border-right: 1px solid var(--border-strong);
}

.title {
  font-weight: bold;
  color: var(--text-h);
  padding-left: 20px;
  align-self: center;
  text-align: left;
}

.function-box {
  display: flex;
  align-content: center;
  flex: 1;
}

.function-container {
  flex: 1;
  display: flex;
  flex-direction: row-reverse;
  align-self: center;
  padding-right: 10px;
}
</style>

<template>
  <div class="top-layout">
    <div class="title-box">
      <h4 class="title">
        OAuth2
      </h4>
    </div>
    <div class="function-box">
      <div class="function-container">
        <LanguageSelector />
        <ThemeSelector />
      </div>
    </div>
  </div>
</template>