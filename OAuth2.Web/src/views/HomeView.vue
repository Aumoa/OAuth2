<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import Avatar from '../shared/oauth2/components/Avatar.vue';
import { useAuthStore } from '../shared/oauth2/src/auth.ts';

const auth = useAuthStore();
const { t } = useI18n();
const displayName = computed(() => (
  auth.user?.name
  ?? auth.user?.id
  ?? auth.user?.sub
  ?? t('app.accountInformation.unknownUser')
));
const profileSummary = computed(() => (
  auth.user?.id
  ?? auth.user?.email
  ?? auth.user?.sub
  ?? ''
));
</script>

<style lang="css" scoped>
.account-page {
  width: min(100%, 880px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.account-page-header {
  margin-bottom: 20px;
}

.account-title {
  margin: 0;
  color: var(--text-h);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.account-description {
  margin-top: 7px;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.profile-panel {
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
}

.profile-summary {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 16px;
  align-items: center;
  padding: 22px 24px;
  border-bottom: 1px solid var(--border);
  background: color-mix(in srgb, var(--accent-bg) 45%, transparent);
}

.profile-heading {
  min-width: 0;
}

.profile-section-label {
  margin: 0 0 3px;
  color: var(--text-muted);
  font-size: 12px;
  font-weight: 600;
  line-height: 1.35;
}

.profile-name,
.profile-summary-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.profile-name {
  margin: 0;
  color: var(--text-h);
  font-size: 20px;
  font-weight: 700;
  line-height: 1.35;
}

.profile-summary-text {
  margin-top: 3px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.4;
}

.profile-details {
  margin: 0;
}

.profile-field {
  display: grid;
  grid-template-columns: minmax(140px, 180px) minmax(0, 1fr);
  gap: 20px;
  align-items: center;
  min-height: 56px;
  padding: 10px 24px;
  box-sizing: border-box;
}

.profile-field + .profile-field {
  border-top: 1px solid var(--border);
}

.profile-field dt {
  display: flex;
  gap: 9px;
  align-items: center;
  color: var(--text-muted);
  font-size: 13px;
  font-weight: 600;
}

.profile-field dt .material-symbols-outlined {
  font-size: 19px;
}

.profile-field dd {
  min-width: 0;
  margin: 0;
  color: var(--text-h);
  font-size: 14px;
  line-height: 1.45;
  overflow-wrap: anywhere;
}

.email-value {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.verification-badge {
  display: inline-flex;
  gap: 3px;
  align-items: center;
  padding: 2px 7px;
  border-radius: 999px;
  color: var(--text-muted);
  background: var(--surface-muted);
  font-size: 11px;
  font-weight: 700;
  white-space: nowrap;
}

.verification-badge.verified {
  color: var(--success);
  background: color-mix(in srgb, var(--success) 12%, transparent);
}

.verification-badge .material-symbols-outlined {
  font-size: 14px;
}

.subject-value {
  font-family: var(--mono);
  font-size: 12px;
}

@media (max-width: 640px) {
  .account-page {
    padding-top: 8px;
  }

  .profile-summary {
    padding: 18px;
  }

  .profile-field {
    grid-template-columns: 1fr;
    gap: 5px;
    padding: 13px 18px;
  }
}
</style>

<template>
  <section class="account-page" aria-labelledby="account-information-title">
    <header class="account-page-header">
      <h1 id="account-information-title" class="account-title">
        {{ t('app.accountInformation.title') }}
      </h1>
      <p class="account-description">
        {{ t('app.accountInformation.description') }}
      </p>
    </header>

    <article v-if="auth.user" class="profile-panel">
      <div class="profile-summary">
        <Avatar size="large" :alt="displayName" />

        <div class="profile-heading">
          <p class="profile-section-label">
            {{ t('app.accountInformation.profileSection') }}
          </p>
          <h2 class="profile-name" :title="displayName">{{ displayName }}</h2>
          <p class="profile-summary-text" :title="profileSummary">{{ profileSummary }}</p>
        </div>
      </div>

      <dl class="profile-details">
        <div class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">badge</span>
            {{ t('app.accountInformation.fields.id') }}
          </dt>
          <dd>{{ auth.user.id ?? t('app.accountInformation.notProvided') }}</dd>
        </div>

        <div class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">mail</span>
            {{ t('app.accountInformation.fields.email') }}
          </dt>
          <dd class="email-value">
            <span>{{ auth.user.email ?? t('app.accountInformation.notProvided') }}</span>
            <span
              v-if="auth.user.email && auth.user.emailVerified !== undefined"
              class="verification-badge"
              :class="{ verified: auth.user.emailVerified }"
            >
              <span class="material-symbols-outlined" aria-hidden="true">
                {{ auth.user.emailVerified ? 'verified' : 'info' }}
              </span>
              {{ auth.user.emailVerified
                ? t('app.accountInformation.emailVerified')
                : t('app.accountInformation.emailUnverified') }}
            </span>
          </dd>
        </div>

        <div class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">fingerprint</span>
            {{ t('app.accountInformation.fields.subject') }}
          </dt>
          <dd class="subject-value">{{ auth.user.sub }}</dd>
        </div>
      </dl>
    </article>
  </section>
</template>
