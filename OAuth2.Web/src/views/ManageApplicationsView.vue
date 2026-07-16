<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Applications, type ApplicationSummary } from '../api/applications.ts';

type ViewState = 'loading' | 'ready' | 'error';

const { locale, t } = useI18n();
const state = ref<ViewState>('loading');
const applications = ref<ApplicationSummary[]>([]);
const dateFormatter = computed(() => new Intl.DateTimeFormat(locale.value, {
  dateStyle: 'medium',
}));

function formatCreatedAt(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : dateFormatter.value.format(date);
}

async function loadApplicationsAsync(): Promise<void> {
  state.value = 'loading';

  try {
    applications.value = await Applications.listAsync();
    state.value = 'ready';
  } catch {
    state.value = 'error';
  }
}

onMounted(loadApplicationsAsync);
</script>

<style scoped lang="css">
.applications-page {
  width: min(100%, 960px);
  margin: 0 auto;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.applications-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 20px;
}

.applications-heading {
  min-width: 0;
}

.applications-title {
  margin: 0;
  color: var(--text-h);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.applications-description {
  margin-top: 7px;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.create-application-button {
  width: auto;
  height: 40px;
  flex: 0 0 auto;
  padding: 0 14px;
  grid-auto-flow: column;
  gap: 7px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
  text-decoration: none;
  white-space: nowrap;
}

.applications-state {
  display: grid;
  min-height: 250px;
  padding: 32px;
  box-sizing: border-box;
  place-items: center;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
  text-align: center;
}

.state-content {
  display: flex;
  max-width: 400px;
  flex-direction: column;
  align-items: center;
}

.state-icon {
  display: grid;
  width: 54px;
  height: 54px;
  margin-bottom: 15px;
  place-items: center;
  border-radius: 16px;
  color: var(--accent-hover);
  background: var(--accent-bg);
}

.state-icon .material-symbols-outlined {
  font-size: 30px;
}

.state-icon.loading .material-symbols-outlined {
  animation: loading-spin 1s linear infinite;
}

.state-title {
  margin: 0;
  color: var(--text-h);
  font-size: 17px;
  font-weight: 700;
  line-height: 1.4;
}

.state-description {
  margin-top: 6px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.retry-button {
  width: auto;
  min-width: 92px;
  height: 38px;
  margin-top: 18px;
  padding: 0 14px;
  grid-auto-flow: column;
  gap: 6px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
}

.application-list {
  display: flex;
  margin: 0;
  padding: 0;
  flex-direction: column;
  gap: 10px;
  list-style: none;
}

.application-card {
  display: grid;
  grid-template-columns: 46px minmax(0, 1fr) auto auto;
  gap: 14px;
  align-items: center;
  min-height: 76px;
  padding: 14px 18px;
  box-sizing: border-box;
  border: 1px solid var(--border);
  border-radius: 11px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
}

.application-icon {
  display: grid;
  width: 46px;
  height: 46px;
  place-items: center;
  border-radius: 12px;
  color: var(--accent-hover);
  background: var(--accent-bg);
}

.application-icon .material-symbols-outlined {
  font-size: 25px;
}

.application-identity {
  min-width: 0;
}

.application-name,
.application-id {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.application-name {
  margin: 0;
  color: var(--text-h);
  font-size: 15px;
  font-weight: 700;
  line-height: 1.4;
}

.application-id {
  margin-top: 3px;
  color: var(--text-muted);
  font-family: var(--mono);
  font-size: 11px;
  line-height: 1.4;
}

.application-date {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  color: var(--text-muted);
  font-size: 11px;
  line-height: 1.45;
  white-space: nowrap;
}

.application-date time {
  color: var(--text);
  font-size: 12px;
  font-weight: 600;
}

.edit-application-button {
  width: auto;
  height: 38px;
  padding: 0 12px;
  grid-auto-flow: column;
  gap: 6px;
  font: inherit;
  font-size: 12px;
  font-weight: 700;
  text-decoration: none;
  white-space: nowrap;
}

@keyframes loading-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .applications-page {
    padding-top: 8px;
  }

  .applications-header {
    flex-direction: column;
    gap: 14px;
  }

  .create-application-button {
    align-self: stretch;
  }

  .applications-state {
    min-height: 220px;
    padding: 24px 18px;
  }

  .application-card {
    grid-template-columns: 42px minmax(0, 1fr) 40px;
    padding: 13px 14px;
  }

  .application-icon {
    width: 42px;
    height: 42px;
  }

  .application-date {
    grid-column: 2 / 4;
    align-items: flex-start;
  }

  .edit-application-button {
    grid-row: 1;
    grid-column: 3;
    width: 40px;
    padding: 0;
  }

  .edit-application-label {
    display: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .state-icon.loading .material-symbols-outlined {
    animation: none;
  }
}
</style>

<template>
  <section class="applications-page" aria-labelledby="applications-title">
    <header class="applications-header">
      <div class="applications-heading">
        <h1 id="applications-title" class="applications-title">
          {{ t('app.applicationManagement.title') }}
        </h1>
        <p class="applications-description">
          {{ t('app.applicationManagement.description') }}
        </p>
      </div>

      <RouterLink
        class="app-button create-application-button"
        to="/applications/new"
      >
        <span class="material-symbols-outlined" aria-hidden="true">add</span>
        <span>{{ t('app.applicationManagement.createAction') }}</span>
      </RouterLink>
    </header>

    <div v-if="state === 'loading'" class="applications-state" role="status">
      <div class="state-content">
        <span class="state-icon loading" aria-hidden="true">
          <span class="material-symbols-outlined">progress_activity</span>
        </span>
        <p class="state-title">{{ t('app.applicationManagement.loading') }}</p>
      </div>
    </div>

    <div v-else-if="state === 'error'" class="applications-state" role="alert">
      <div class="state-content">
        <span class="state-icon" aria-hidden="true">
          <span class="material-symbols-outlined">cloud_off</span>
        </span>
        <p class="state-title">{{ t('app.applicationManagement.loadFailed') }}</p>
        <button type="button" class="app-button retry-button" @click="loadApplicationsAsync">
          <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
          <span>{{ t('app.applicationManagement.retry') }}</span>
        </button>
      </div>
    </div>

    <div v-else-if="applications.length === 0" class="applications-state" role="status">
      <div class="state-content">
        <span class="state-icon" aria-hidden="true">
          <span class="material-symbols-outlined">apps</span>
        </span>
        <h2 class="state-title">{{ t('app.applicationManagement.emptyTitle') }}</h2>
        <p class="state-description">{{ t('app.applicationManagement.emptyDescription') }}</p>
      </div>
    </div>

    <ul v-else class="application-list">
      <li v-for="application in applications" :key="application.id">
        <article class="application-card">
          <span class="application-icon" aria-hidden="true">
            <span class="material-symbols-outlined">web_asset</span>
          </span>

          <div class="application-identity">
            <h2 class="application-name" :title="application.name">{{ application.name }}</h2>
            <p class="application-id" :title="application.id">
              {{ t('app.applicationManagement.clientId') }}: {{ application.id }}
            </p>
          </div>

          <div class="application-date">
            <span>{{ t('app.applicationManagement.createdAt') }}</span>
            <time :datetime="application.createdAt">
              {{ formatCreatedAt(application.createdAt) }}
            </time>
          </div>

          <RouterLink
            class="app-button edit-application-button"
            :to="{ name: 'applications-edit', params: { clientId: application.id } }"
            :aria-label="t('app.applicationManagement.editApplicationLabel', { name: application.name })"
          >
            <span class="material-symbols-outlined" aria-hidden="true">edit</span>
            <span class="edit-application-label">{{ t('app.applicationManagement.editAction') }}</span>
          </RouterLink>
        </article>
      </li>
    </ul>

  </section>
</template>
