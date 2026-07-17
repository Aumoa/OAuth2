import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { Organizations, type OrganizationSummary } from '../api/Organizations.ts';

type OrganizationsStatus = 'idle' | 'loading' | 'loaded' | 'failed';

export const useOrganizationsStore = defineStore('organizations', () => {
  const organizations = ref<OrganizationSummary[]>([]);
  const status = ref<OrganizationsStatus>('idle');
  let loadingPromise: Promise<void> | null = null;

  const isLoading = computed(() => status.value === 'loading');
  const hasFailed = computed(() => status.value === 'failed');

  async function loadAsync(force = false): Promise<void> {
    if (!force && status.value === 'loaded') {
      return;
    }

    if (loadingPromise !== null) {
      return await loadingPromise;
    }

    status.value = 'loading';
    loadingPromise = (async () => {
      try {
        organizations.value = await Organizations.listAsync();
        status.value = 'loaded';
      } catch (error) {
        status.value = 'failed';
        throw error;
      } finally {
        loadingPromise = null;
      }
    })();

    return await loadingPromise;
  }

  async function createAsync(id: string, name: string): Promise<OrganizationSummary> {
    const organization = await Organizations.createAsync(id, name);
    const existingIndex = organizations.value.findIndex(item => item.id === organization.id);
    if (existingIndex >= 0) {
      organizations.value.splice(existingIndex, 1, organization);
    } else {
      organizations.value.push(organization);
    }

    status.value = 'loaded';
    return organization;
  }

  function find(id: string): OrganizationSummary | undefined {
    return organizations.value.find(organization => organization.id === id);
  }

  return {
    organizations,
    status,
    isLoading,
    hasFailed,
    loadAsync,
    createAsync,
    find,
  };
});
