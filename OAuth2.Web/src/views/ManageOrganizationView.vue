<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import { Organizations, type OrganizationSummary } from '../api/Organizations.ts';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

const { locale, t } = useI18n();
const route = useRoute();
const organization = ref<OrganizationSummary | null>(null);
const isLoading = ref(true);
const loadFailed = ref(false);
const notFound = ref(false);
let isMounted = true;

const organizationId = computed(() => {
  const value = route.params.organizationId;
  return Array.isArray(value) ? value.join('/') : value ?? '';
});
const createdAt = computed(() => {
  if (organization.value === null) {
    return '';
  }

  return new Intl.DateTimeFormat(locale.value, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(organization.value.createdAt));
});

async function loadOrganizationAsync(): Promise<void> {
  isLoading.value = true;
  loadFailed.value = false;
  notFound.value = false;
  organization.value = null;

  try {
    const result = await Organizations.getAsync(organizationId.value);
    if (isMounted) {
      organization.value = result;
    }
  } catch (error) {
    if (!isMounted) {
      return;
    }

    if (error instanceof HttpStatusCodeError && error.status === 404) {
      notFound.value = true;
    } else {
      loadFailed.value = true;
    }
  } finally {
    if (isMounted) {
      isLoading.value = false;
    }
  }
}

watch(organizationId, loadOrganizationAsync);

onMounted(loadOrganizationAsync);

onBeforeUnmount(() => {
  isMounted = false;
});
</script>

<style scoped lang="css">
.organization-page {
  --organization-accent: #a78bfa;
  --organization-accent-strong: #8b5cf6;

  width: min(100%, 960px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.organization-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 20px;
}

.organization-title {
  margin: 0;
  color: color-mix(in srgb, var(--organization-accent) 72%, var(--text-h));
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.organization-description {
  margin: 7px 0 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.organization-applications-link {
  display: inline-flex;
  min-height: 38px;
  padding: 0 13px;
  align-items: center;
  gap: 7px;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 40%, var(--border));
  border-radius: 8px;
  color: color-mix(in srgb, var(--organization-accent) 82%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 9%, var(--surface));
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
}

.organization-applications-link:hover {
  border-color: color-mix(in srgb, var(--organization-accent) 68%, var(--border));
  background: color-mix(in srgb, var(--organization-accent-strong) 15%, var(--surface));
}

.organization-panel,
.organization-state {
  width: min(100%, 680px);
  padding: 22px;
  box-sizing: border-box;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 32%, var(--border));
  border-radius: 12px;
  background: color-mix(in srgb, var(--organization-accent-strong) 7%, var(--surface));
  box-shadow: var(--shadow-sm);
}

.organization-section-title,
.state-title {
  margin: 0;
  color: var(--text-h);
  font-size: 16px;
  font-weight: 750;
}

.organization-fields {
  display: grid;
  gap: 0;
  margin: 14px 0 0;
}

.organization-field {
  display: grid;
  grid-template-columns: 148px minmax(0, 1fr);
  gap: 16px;
  padding: 13px 0;
  border-top: 1px solid color-mix(in srgb, var(--organization-accent) 18%, var(--border));
}

.organization-field dt {
  color: var(--text-muted);
  font-size: 12px;
  font-weight: 700;
}

.organization-field dd {
  min-width: 0;
  margin: 0;
  overflow-wrap: anywhere;
  color: var(--text);
  font-size: 13px;
}

.organization-id {
  font-family: var(--mono);
}

.organization-role {
  display: inline-flex;
  width: fit-content;
  padding: 2px 8px;
  border-radius: 999px;
  color: color-mix(in srgb, var(--organization-accent) 85%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 14%, transparent);
  font-size: 11px;
  font-weight: 800;
  text-transform: uppercase;
}

.organization-state {
  color: var(--text-muted);
  font-size: 13px;
}

.state-description {
  margin: 5px 0 0;
  line-height: 1.5;
}

@media (max-width: 640px) {
  .organization-page {
    padding-top: 8px;
  }

  .organization-header {
    flex-direction: column;
  }

  .organization-field {
    grid-template-columns: 1fr;
    gap: 4px;
  }
}
</style>

<template>
  <section class="organization-page" aria-labelledby="organization-title">
    <header class="organization-header">
      <div>
        <h1 id="organization-title" class="organization-title">
          {{ organization?.name ?? t('app.organizationManagement.title') }}
        </h1>
        <p class="organization-description">
          {{ t('app.organizationManagement.description') }}
        </p>
      </div>

      <RouterLink
        v-if="organization"
        class="organization-applications-link"
        :to="{
          name: 'applications-organization',
          params: { organizationId: organization.id },
        }"
      >
        <span class="material-symbols-outlined" aria-hidden="true">apps</span>
        <span>{{ t('app.organizationManagement.manageApplications') }}</span>
      </RouterLink>
    </header>

    <div v-if="isLoading" class="organization-state" role="status">
      {{ t('app.organizationManagement.loading') }}
    </div>

    <div v-else-if="loadFailed" class="organization-state" role="alert">
      <h2 class="state-title">{{ t('app.organizationManagement.loadFailed') }}</h2>
      <p class="state-description">{{ t('app.organizationManagement.loadFailedDescription') }}</p>
    </div>

    <div v-else-if="notFound" class="organization-state">
      <h2 class="state-title">{{ t('app.organizationManagement.notFound') }}</h2>
      <p class="state-description">{{ t('app.organizationManagement.notFoundDescription') }}</p>
    </div>

    <section v-else-if="organization" class="organization-panel">
      <h2 class="organization-section-title">{{ t('app.organizationManagement.basicInformation') }}</h2>
      <dl class="organization-fields">
        <div class="organization-field">
          <dt>{{ t('app.organizationManagement.name') }}</dt>
          <dd>{{ organization.name }}</dd>
        </div>
        <div class="organization-field">
          <dt>{{ t('app.organizationManagement.id') }}</dt>
          <dd class="organization-id">{{ organization.id }}</dd>
        </div>
        <div class="organization-field">
          <dt>{{ t('app.organizationManagement.role') }}</dt>
          <dd><span class="organization-role">{{ organization.role }}</span></dd>
        </div>
        <div class="organization-field">
          <dt>{{ t('app.organizationManagement.createdAt') }}</dt>
          <dd>{{ createdAt }}</dd>
        </div>
      </dl>
    </section>
  </section>
</template>
