<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useOrganizationsStore } from '../stores/organizations.ts';

const organizationsStore = useOrganizationsStore();
const { locale, t } = useI18n();
const organizations = computed(() => {
  const collator = new Intl.Collator(locale.value, { sensitivity: 'base' });
  return [...organizationsStore.organizations]
    .sort((left, right) => collator.compare(left.name, right.name));
});

onMounted(() => {
  void organizationsStore.loadAsync().catch(() => undefined);
});
</script>

<style scoped lang="css">
.application-groups-page {
  width: min(100%, 960px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.application-groups-header {
  margin-bottom: 20px;
}

.application-groups-title {
  margin: 0;
  color: var(--text-h);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.application-groups-description {
  margin: 7px 0 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.application-group-link {
  display: grid;
  grid-template-columns: 46px minmax(0, 1fr) auto;
  gap: 14px;
  align-items: center;
  min-height: 78px;
  padding: 14px 18px;
  box-sizing: border-box;
  border: 1px solid var(--border);
  border-radius: 12px;
  color: var(--text);
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
  text-decoration: none;
  transition:
    border-color 160ms ease,
    background-color 160ms ease,
    box-shadow 160ms ease,
    transform 160ms ease;
}

.application-group-link:hover {
  border-color: var(--accent-border);
  background: color-mix(in srgb, var(--accent-bg) 52%, var(--surface));
  box-shadow: var(--shadow-sm), inset 3px 0 0 var(--accent);
  transform: translateY(-1px);
}

.application-group-link:focus-visible {
  outline: 3px solid var(--focus-ring);
  outline-offset: 2px;
}

.application-group-icon {
  display: grid;
  width: 46px;
  height: 46px;
  place-items: center;
  border-radius: 12px;
  color: var(--accent-hover);
  background: var(--accent-bg);
}

.application-group-icon .material-symbols-outlined {
  font-size: 25px;
}

.application-group-copy {
  min-width: 0;
}

.application-group-name {
  display: block;
  margin: 0;
  overflow: hidden;
  color: var(--text-h);
  font-size: 15px;
  font-weight: 700;
  line-height: 1.4;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.application-group-description {
  display: block;
  margin: 3px 0 0;
  color: var(--text-muted);
  font-size: 12px;
  line-height: 1.45;
}

.application-group-arrow {
  color: var(--text-muted);
  font-size: 22px;
  transition: transform 160ms ease;
}

.application-group-link:hover .application-group-arrow {
  transform: translateX(2px);
}

.organization-groups {
  --organization-accent: #a78bfa;
  --organization-accent-strong: #8b5cf6;
  --organization-background: color-mix(in srgb, var(--organization-accent-strong) 8%, var(--surface));

  margin-top: 18px;
  padding: 18px;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 42%, var(--border));
  border-radius: 14px;
  background: var(--organization-background);
  box-shadow: var(--shadow-sm);
}

.organization-groups-header {
  display: flex;
  gap: 14px;
  align-items: flex-start;
  margin-bottom: 14px;
}

.organization-groups-heading {
  min-width: 0;
}

.organization-groups-title {
  margin: 0;
  color: color-mix(in srgb, var(--organization-accent) 78%, var(--text-h));
  font-size: 16px;
  font-weight: 750;
  line-height: 1.4;
}

.organization-groups-description {
  margin: 4px 0 0;
  color: var(--text-muted);
  font-size: 12px;
  line-height: 1.5;
}

.organization-count {
  display: inline-flex;
  min-width: 24px;
  height: 24px;
  margin-left: auto;
  padding: 0 7px;
  box-sizing: border-box;
  align-items: center;
  justify-content: center;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 44%, transparent);
  border-radius: 999px;
  color: color-mix(in srgb, var(--organization-accent) 84%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 13%, transparent);
  font-size: 11px;
  font-weight: 800;
}

.organization-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.organization-link {
  border-color: color-mix(in srgb, var(--organization-accent) 34%, var(--border));
  background: color-mix(in srgb, var(--surface) 91%, var(--organization-accent-strong));
}

.organization-link:hover {
  border-color: color-mix(in srgb, var(--organization-accent) 72%, var(--border));
  background: color-mix(in srgb, var(--organization-accent-strong) 13%, var(--surface));
  box-shadow: var(--shadow-sm), inset 3px 0 0 var(--organization-accent-strong);
}

.organization-link .application-group-icon {
  color: color-mix(in srgb, var(--organization-accent) 86%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 15%, transparent);
}

.organization-empty {
  display: flex;
  min-height: 76px;
  padding: 14px 16px;
  box-sizing: border-box;
  align-items: center;
  gap: 12px;
  border: 1px dashed color-mix(in srgb, var(--organization-accent) 34%, var(--border));
  border-radius: 11px;
  color: var(--text-muted);
  background: color-mix(in srgb, var(--surface) 86%, transparent);
}

.organization-empty .material-symbols-outlined {
  color: color-mix(in srgb, var(--organization-accent) 68%, var(--text-muted));
  font-size: 25px;
}

.organization-empty-copy {
  min-width: 0;
}

.organization-empty-title {
  margin: 0;
  color: var(--text);
  font-size: 13px;
  font-weight: 700;
}

.organization-empty-description {
  margin: 3px 0 0;
  font-size: 12px;
  line-height: 1.45;
}

@media (max-width: 720px) {
  .application-groups-page {
    padding-top: 8px;
  }

  .organization-list {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 480px) {
  .application-group-link {
    grid-template-columns: 42px minmax(0, 1fr) auto;
    padding: 13px 14px;
  }

  .application-group-icon {
    width: 42px;
    height: 42px;
  }

  .organization-groups {
    padding: 14px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .application-group-link,
  .application-group-arrow {
    transition-duration: 0.01ms;
  }
}
</style>

<template>
  <section class="application-groups-page" aria-labelledby="application-groups-title">
    <header class="application-groups-header">
      <h1 id="application-groups-title" class="application-groups-title">
        {{ t('app.applicationManagement.title') }}
      </h1>
      <p class="application-groups-description">
        {{ t('app.applicationManagement.description') }}
      </p>
    </header>

    <RouterLink
      class="application-group-link personal-link"
      :to="{ name: 'applications-personal' }"
    >
      <span class="application-group-icon" aria-hidden="true">
        <span class="material-symbols-outlined">person</span>
      </span>
      <span class="application-group-copy">
        <span class="application-group-name">
          {{ t('app.applicationManagement.personalGroupTitle') }}
        </span>
        <span class="application-group-description">
          {{ t('app.applicationManagement.personalGroupDescription') }}
        </span>
      </span>
      <span class="material-symbols-outlined application-group-arrow" aria-hidden="true">
        chevron_right
      </span>
    </RouterLink>

    <section class="organization-groups" aria-labelledby="organization-groups-title">
      <header class="organization-groups-header">
        <div class="organization-groups-heading">
          <h2 id="organization-groups-title" class="organization-groups-title">
            {{ t('app.applicationManagement.organizationGroupsTitle') }}
          </h2>
          <p class="organization-groups-description">
            {{ t('app.applicationManagement.organizationGroupsDescription') }}
          </p>
        </div>
        <span class="organization-count" :aria-label="t('app.applicationManagement.organizationCount', { count: organizations.length })">
          {{ organizations.length }}
        </span>
      </header>

      <ul v-if="organizations.length > 0" class="organization-list">
        <li v-for="organization in organizations" :key="organization.id">
          <RouterLink
            class="application-group-link organization-link"
            :to="{
              name: 'applications-organization',
              params: { organizationId: organization.id },
            }"
          >
            <span class="application-group-icon" aria-hidden="true">
              <span class="material-symbols-outlined">domain</span>
            </span>
            <span class="application-group-copy">
              <span class="application-group-name" :title="organization.name">
                {{ organization.name }}
              </span>
              <span class="application-group-description">
                {{ t('app.applicationManagement.organizationGroupDescription') }}
              </span>
            </span>
            <span class="material-symbols-outlined application-group-arrow" aria-hidden="true">
              chevron_right
            </span>
          </RouterLink>
        </li>
      </ul>

      <div v-else class="organization-empty" role="status">
        <span class="material-symbols-outlined" aria-hidden="true">domain_disabled</span>
        <div class="organization-empty-copy">
          <p class="organization-empty-title">
            {{ organizationsStore.isLoading
              ? t('app.applicationManagement.organizationLoadingTitle')
              : organizationsStore.hasFailed
                ? t('app.applicationManagement.organizationLoadFailedTitle')
                : t('app.applicationManagement.organizationEmptyTitle') }}
          </p>
          <p class="organization-empty-description">
            {{ organizationsStore.isLoading
              ? t('app.applicationManagement.organizationLoadingDescription')
              : organizationsStore.hasFailed
                ? t('app.applicationManagement.organizationLoadFailedDescription')
                : t('app.applicationManagement.organizationEmptyDescription') }}
          </p>
        </div>
      </div>
    </section>
  </section>
</template>
