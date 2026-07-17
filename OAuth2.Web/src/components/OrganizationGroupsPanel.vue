<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  OrganizationGroups,
  type OrganizationGroupSummary,
} from '../api/OrganizationGroups.ts';
import type { OrganizationRole } from '../api/Organizations.ts';

const props = defineProps<{
  organizationId: string;
  role: OrganizationRole;
}>();

const { t } = useI18n();
const groups = ref<OrganizationGroupSummary[]>([]);
const isLoading = ref(false);
const loadFailed = ref(false);
let requestId = 0;

const canManage = computed(() => props.role === 'owner' || props.role === 'admin');

async function loadAsync(): Promise<void> {
  const currentRequestId = ++requestId;
  isLoading.value = true;
  loadFailed.value = false;
  try {
    const result = await OrganizationGroups.listAsync(props.organizationId);
    if (currentRequestId === requestId) {
      groups.value = result;
    }
  } catch {
    if (currentRequestId === requestId) {
      loadFailed.value = true;
    }
  } finally {
    if (currentRequestId === requestId) {
      isLoading.value = false;
    }
  }
}

watch(() => props.organizationId, loadAsync, { immediate: true });
</script>

<style scoped lang="css">
.groups-panel {
  --organization-accent: #a78bfa;
  --organization-accent-strong: #8b5cf6;

  width: 100%;
  margin-top: 20px;
  padding: 22px;
  box-sizing: border-box;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 28%, var(--border));
  border-radius: 12px;
  background: color-mix(in srgb, var(--organization-accent-strong) 5%, var(--surface));
  box-shadow: var(--shadow-sm);
}

.section-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
}

.section-title {
  margin: 0;
  color: var(--text-h);
  font-size: 16px;
  font-weight: 750;
}

.section-description {
  margin: 5px 0 0;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.group-count {
  flex: 0 0 auto;
  padding: 3px 9px;
  border-radius: 999px;
  color: color-mix(in srgb, var(--organization-accent) 85%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 13%, transparent);
  font-size: 12px;
  font-weight: 800;
}

.section-actions {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  gap: 9px;
}

.create-group-button {
  width: auto;
  min-height: 38px;
  padding: 0 13px;
  grid-auto-flow: column;
  gap: 6px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
  white-space: nowrap;
}

.create-group-button .material-symbols-outlined {
  font-size: 18px;
}

.groups-state {
  margin-top: 18px;
  padding: 16px;
  border: 1px dashed color-mix(in srgb, var(--organization-accent) 22%, var(--border));
  border-radius: 9px;
  color: var(--text-muted);
  font-size: 13px;
}

.group-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 10px;
  margin-top: 18px;
}

.group-card {
  display: flex;
  min-width: 0;
  padding: 14px;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 24%, var(--border));
  border-radius: 10px;
  color: inherit;
  background: color-mix(in srgb, var(--surface) 96%, transparent);
  text-decoration: none;
  transition: border-color 150ms ease, background-color 150ms ease;
}

.group-card:hover {
  border-color: color-mix(in srgb, var(--organization-accent) 62%, var(--border));
  background: color-mix(in srgb, var(--organization-accent-strong) 10%, var(--surface));
}

.group-identity {
  min-width: 0;
}

.group-name,
.group-id {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.group-name {
  color: var(--text-h);
  font-size: 14px;
  font-weight: 750;
}

.group-id {
  margin-top: 3px;
  color: var(--text-muted);
  font-family: var(--mono);
  font-size: 11px;
}

.group-meta {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  gap: 5px;
  color: color-mix(in srgb, var(--organization-accent) 84%, var(--text-h));
  font-size: 11px;
  font-weight: 700;
}

.group-meta .material-symbols-outlined {
  font-size: 17px;
}

@media (max-width: 640px) {
  .section-heading {
    flex-direction: column;
  }

  .section-actions {
    width: 100%;
    justify-content: space-between;
  }
}
</style>

<template>
  <section class="groups-panel" aria-labelledby="organization-groups-title">
    <div class="section-heading">
      <div>
        <h2 id="organization-groups-title" class="section-title">
          {{ t('app.organizationManagement.groups.title') }}
        </h2>
        <p class="section-description">
          {{ t('app.organizationManagement.groups.description') }}
        </p>
      </div>
      <div class="section-actions">
        <span class="group-count">{{ groups.length }}</span>
        <RouterLink
          v-if="canManage"
          class="app-button create-group-button"
          :to="{
            name: 'organization-group-new',
            params: { organizationId },
          }"
        >
          <span class="material-symbols-outlined" aria-hidden="true">add</span>
          <span>{{ t('app.organizationManagement.groups.createAction') }}</span>
        </RouterLink>
      </div>
    </div>

    <div v-if="isLoading" class="groups-state" role="status">
      {{ t('app.organizationManagement.groups.loading') }}
    </div>
    <div v-else-if="loadFailed" class="groups-state" role="alert">
      {{ t('app.organizationManagement.groups.loadFailed') }}
    </div>
    <div v-else-if="groups.length === 0" class="groups-state">
      {{ t('app.organizationManagement.groups.empty') }}
    </div>
    <div v-else class="group-list">
      <RouterLink
        v-for="group in groups"
        :key="group.id"
        class="group-card"
        :to="{
          name: 'organization-group',
          params: { organizationId: group.organizationId, groupId: group.id },
        }"
      >
        <span class="group-identity">
          <span class="group-name">{{ group.name }}</span>
          <span class="group-id">{{ group.id }}</span>
        </span>
        <span class="group-meta">
          <span class="material-symbols-outlined" aria-hidden="true">group</span>
          {{ group.memberCount }}
        </span>
      </RouterLink>
    </div>
  </section>
</template>
