<script setup lang="ts">
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Accounts } from '../api/accounts.ts';
import ProfileImageEditor from '../components/ProfileImageEditor.vue';
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
const groups = computed(() => auth.user?.groups ?? []);
const hasProfileImage = computed(() => Boolean(auth.user?.picture));
const isProfileImageEditorOpen = ref(false);
const isSavingProfileImage = ref(false);
const profileImageError = ref<string | null>(null);

function openProfileImageEditor(): void {
  profileImageError.value = null;
  isProfileImageEditorOpen.value = true;
}

function updateProfileImageEditorOpen(value: boolean): void {
  if (!isSavingProfileImage.value) {
    isProfileImageEditorOpen.value = value;
    if (!value) {
      profileImageError.value = null;
    }
  }
}

async function saveProfileImageAsync(image: Blob): Promise<void> {
  if (isSavingProfileImage.value) {
    return;
  }

  isSavingProfileImage.value = true;
  profileImageError.value = null;
  try {
    const reference = await Accounts.updateProfileImageAsync(image);
    auth.setPicture(reference.picture);
    isProfileImageEditorOpen.value = false;
  } catch {
    profileImageError.value = t('app.accountInformation.profileImage.uploadFailed');
  } finally {
    isSavingProfileImage.value = false;
  }
}

async function removeProfileImageAsync(): Promise<void> {
  if (
    isSavingProfileImage.value
    || !window.confirm(t('app.accountInformation.profileImage.removeConfirmation'))
  ) {
    return;
  }

  isSavingProfileImage.value = true;
  profileImageError.value = null;
  try {
    await Accounts.deleteProfileImageAsync();
    auth.setPicture(undefined);
    isProfileImageEditorOpen.value = false;
  } catch {
    profileImageError.value = t('app.accountInformation.profileImage.removeFailed');
  } finally {
    isSavingProfileImage.value = false;
  }
}
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

.profile-avatar-editor {
  position: relative;
}

.profile-image-edit-button {
  position: absolute;
  right: -6px;
  bottom: -6px;
  display: grid;
  width: 28px;
  height: 28px;
  padding: 0;
  place-items: center;
  color: var(--on-accent);
  border: 2px solid var(--surface);
  border-radius: 50%;
  background: var(--accent);
  box-shadow: var(--shadow-sm);
  cursor: pointer;
  transition: transform 140ms ease, filter 140ms ease;
}

.profile-image-edit-button:hover {
  filter: brightness(1.08);
  transform: scale(1.06);
}

.profile-image-edit-button:focus-visible {
  outline: 3px solid var(--accent-border);
  outline-offset: 2px;
}

.profile-image-edit-button .material-symbols-outlined {
  font-size: 16px;
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

.groups-claim-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.groups-claim-tag {
  display: inline-flex;
  min-width: 0;
  max-width: 100%;
  padding: 4px 9px 4px 6px;
  align-items: center;
  gap: 4px;
  border: 1px solid color-mix(in srgb, var(--success) 36%, var(--border));
  border-radius: 999px;
  color: color-mix(in srgb, var(--success) 84%, var(--text-h));
  background: color-mix(in srgb, var(--success) 11%, transparent);
  font-family: var(--mono);
  font-size: 11px;
  font-weight: 700;
  line-height: 1.35;
  overflow-wrap: anywhere;
}

.groups-claim-tag .material-symbols-outlined {
  flex: 0 0 auto;
  font-size: 14px;
}

.groups-claim-tag > span:last-child {
  min-width: 0;
  overflow-wrap: anywhere;
}

.groups-claim-empty {
  color: var(--text-muted);
  font-size: 13px;
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
        <div class="profile-avatar-editor">
          <Avatar size="large" :alt="displayName" />
          <button
            type="button"
            class="app-button profile-image-edit-button"
            :aria-label="t('app.accountInformation.profileImage.editAction')"
            :title="t('app.accountInformation.profileImage.editAction')"
            @click="openProfileImageEditor"
          >
            <span class="material-symbols-outlined" aria-hidden="true">photo_camera</span>
          </button>
        </div>

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

        <div class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">groups</span>
            {{ t('app.accountInformation.fields.groups') }}
          </dt>
          <dd class="groups-claim-tags">
            <span v-for="group in groups" :key="group" class="groups-claim-tag">
              <span class="material-symbols-outlined" aria-hidden="true">label</span>
              <span>{{ group }}</span>
            </span>
            <span v-if="groups.length === 0" class="groups-claim-empty">
              {{ t('app.accountInformation.notProvided') }}
            </span>
          </dd>
        </div>
      </dl>
    </article>

    <ProfileImageEditor
      :is-open="isProfileImageEditorOpen"
      :is-saving="isSavingProfileImage"
      :has-image="hasProfileImage"
      :error="profileImageError"
      @update:is-open="updateProfileImageEditorOpen"
      @save="saveProfileImageAsync"
      @remove="removeProfileImageAsync"
    />
  </section>
</template>
