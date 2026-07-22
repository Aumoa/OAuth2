<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  Accounts,
  type AccountProfile,
  type AccountProfileClaim,
} from '../api/accounts.ts';
import ProfileImageEditor from '../components/ProfileImageEditor.vue';
import Dialog from '../core/components/Dialog.vue';
import Avatar from '../shared/oauth2/components/Avatar.vue';
import { useAuthStore } from '../shared/oauth2/src/auth.ts';

const auth = useAuthStore();
const { t } = useI18n();

const claimTypes = [
  'family_name',
  'given_name',
  'middle_name',
  'profile',
  'website',
  'gender',
  'birthdate',
  'zoneinfo',
  'locale',
  'phone_number',
] as const;

type ClaimType = typeof claimTypes[number];
type ProfileDialogMode = 'fullName' | 'nickname' | 'claim' | 'add';

const profile = ref<AccountProfile | null>(null);
const isLoadingProfile = ref(false);
const isSavingProfile = ref(false);
const profileError = ref<string | null>(null);
const profileDialogMode = ref<ProfileDialogMode | null>(null);
const profileDialogClaimName = ref<ClaimType>('family_name');
const profileDialogValue = ref('');
const profileDialogError = ref<string | null>(null);

const displayName = computed(() => (
  profile.value?.nickname
  ?? profile.value?.fullName
  ?? auth.user?.nickname
  ?? auth.user?.name
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

const shownFullName = computed(() => (
  profile.value?.fullName
  ?? auth.user?.name
  ?? t('app.accountInformation.notProvided')
));
const shownNickname = computed(() => (
  profile.value?.nickname
  ?? auth.user?.nickname
  ?? t('app.accountInformation.notProvided')
));
const availableClaimTypes = computed(() => {
  const savedTypes = new Set(profile.value?.claims.map((claim) => claim.name) ?? []);
  return claimTypes.filter((name) => !savedTypes.has(name));
});
const isProfileDialogOpen = computed(() => profileDialogMode.value !== null);
const profileDialogTitle = computed(() => {
  if (profileDialogMode.value === 'add') {
    return t('app.accountInformation.profile.addDialogTitle');
  }

  const label = profileDialogMode.value === 'fullName'
    ? t('app.accountInformation.fields.fullName')
    : profileDialogMode.value === 'nickname'
      ? t('app.accountInformation.fields.nickname')
      : claimLabel(profileDialogClaimName.value);
  return t('app.accountInformation.profile.editDialogTitle', { type: label });
});
const profileDialogFieldLabel = computed(() => (
  profileDialogMode.value === 'fullName'
    ? t('app.accountInformation.fields.fullName')
    : profileDialogMode.value === 'nickname'
      ? t('app.accountInformation.fields.nickname')
      : claimLabel(profileDialogClaimName.value)
));
const profileDialogMaxLength = computed(() => (
  profileDialogMode.value === 'fullName' || profileDialogMode.value === 'nickname'
    ? 128
    : 2048
));
const canSaveProfileDialog = computed(() => {
  const valueLength = profileDialogValue.value.trim().length;
  if (profileDialogMode.value === 'nickname') {
    return valueLength <= profileDialogMaxLength.value;
  }

  return valueLength > 0 && valueLength <= profileDialogMaxLength.value;
});

function claimLabel(name: string): string {
  return t(`app.accountInformation.claimTypes.${name}`);
}

function openProfileDialog(mode: ProfileDialogMode, claim?: AccountProfileClaim): void {
  if (!profile.value || isLoadingProfile.value) {
    return;
  }

  if (mode === 'add') {
    const firstAvailableType = availableClaimTypes.value[0];
    if (!firstAvailableType) {
      return;
    }

    profileDialogClaimName.value = firstAvailableType;
    profileDialogValue.value = '';
  } else if (mode === 'claim' && claim) {
    profileDialogClaimName.value = claim.name as ClaimType;
    profileDialogValue.value = claim.value;
  } else if (mode === 'fullName') {
    profileDialogValue.value = profile.value.fullName;
  } else if (mode === 'nickname') {
    profileDialogValue.value = profile.value.nickname ?? '';
  }

  profileDialogError.value = null;
  profileDialogMode.value = mode;
}

function updateProfileDialogOpen(value: boolean): void {
  if (!value && !isSavingProfile.value) {
    profileDialogMode.value = null;
    profileDialogError.value = null;
  }
}

async function loadProfileAsync(): Promise<void> {
  if (isLoadingProfile.value) {
    return;
  }

  isLoadingProfile.value = true;
  profileError.value = null;
  try {
    profile.value = await Accounts.getProfileAsync();
    auth.setProfile(profile.value.fullName, profile.value.nickname ?? undefined);
  } catch {
    profileError.value = t('app.accountInformation.profile.loadFailed');
  } finally {
    isLoadingProfile.value = false;
  }
}

async function updateProfileAsync(
  fullName: string,
  nickname: string | null,
  claims: AccountProfileClaim[],
): Promise<boolean> {
  if (isSavingProfile.value) {
    return false;
  }

  isSavingProfile.value = true;
  profileDialogError.value = null;
  try {
    const saved = await Accounts.updateProfileAsync({ fullName, nickname, claims });
    profile.value = saved;
    auth.setProfile(saved.fullName, saved.nickname ?? undefined);
    profileDialogMode.value = null;
    return true;
  } catch {
    profileDialogError.value = t('app.accountInformation.profile.saveFailed');
    return false;
  } finally {
    isSavingProfile.value = false;
  }
}

async function saveProfileDialogAsync(): Promise<void> {
  if (!profile.value || !profileDialogMode.value || !canSaveProfileDialog.value) {
    return;
  }

  const fullName = profileDialogMode.value === 'fullName'
    ? profileDialogValue.value.trim()
    : profile.value.fullName;
  const nickname = profileDialogMode.value === 'nickname'
    ? profileDialogValue.value.trim() || null
    : profile.value.nickname ?? null;
  const claims = profile.value.claims.map((claim) => ({ ...claim }));
  if (profileDialogMode.value === 'claim') {
    const claim = claims.find((item) => item.name === profileDialogClaimName.value);
    if (!claim) {
      return;
    }

    claim.value = profileDialogValue.value.trim();
  } else if (profileDialogMode.value === 'add') {
    claims.push({
      name: profileDialogClaimName.value,
      value: profileDialogValue.value.trim(),
    });
  }

  await updateProfileAsync(fullName, nickname, claims);
}

async function removeProfileClaimAsync(): Promise<void> {
  if (!profile.value || profileDialogMode.value !== 'claim') {
    return;
  }

  const claims = profile.value.claims
    .filter((claim) => claim.name !== profileDialogClaimName.value)
    .map((claim) => ({ ...claim }));
  await updateProfileAsync(
    profile.value.fullName,
    profile.value.nickname ?? null,
    claims,
  );
}

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

onMounted(loadProfileAsync);
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

.profile-notice {
  margin: 0;
  padding: 10px 24px;
  color: var(--danger);
  border-bottom: 1px solid color-mix(in srgb, var(--danger) 25%, var(--border));
  background: color-mix(in srgb, var(--danger) 7%, transparent);
  font-size: 13px;
}

.profile-claim-value {
  white-space: pre-wrap;
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

.profile-row-content {
  display: flex;
  min-width: 0;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
}

.profile-row-value {
  min-width: 0;
  overflow-wrap: anywhere;
}

.profile-row-edit-button {
  display: inline-grid;
  width: 34px;
  min-width: 34px;
  height: 34px;
  flex: 0 0 auto;
  padding: 0;
  place-items: center;
  color: var(--accent);
  border: 1px solid var(--accent-border);
  border-radius: 8px;
  background: var(--surface);
  cursor: pointer;
}

.profile-row-edit-button:hover:not(:disabled),
.profile-add-row-button:hover:not(:disabled) {
  background: var(--accent-bg);
}

.profile-row-edit-button:disabled,
.profile-add-row-button:disabled,
.profile-dialog-action:disabled {
  cursor: default;
  opacity: 0.55;
}

.profile-row-edit-button .material-symbols-outlined {
  font-size: 17px;
}

.profile-add-row-button {
  display: inline-flex;
  gap: 6px;
  align-items: center;
  padding: 7px 11px;
  color: var(--accent);
  border: 1px solid var(--accent-border);
  border-radius: 8px;
  background: var(--surface);
  cursor: pointer;
  font-size: 12px;
  font-weight: 700;
  white-space: nowrap;
}

.profile-add-row-button .material-symbols-outlined {
  font-size: 18px;
}

.profile-dialog-description {
  margin: 0 0 18px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.profile-dialog-form {
  display: grid;
  gap: 14px;
}

.profile-dialog-field {
  display: grid;
  gap: 7px;
  color: var(--text-muted);
  font-size: 12px;
  font-weight: 700;
}

.profile-dialog-input,
.profile-dialog-select {
  width: 100%;
  min-width: 0;
  height: 42px;
  padding: 0 12px;
  box-sizing: border-box;
  color: var(--text-h);
  border: 1px solid var(--border);
  border-radius: 9px;
  outline: none;
  background: var(--surface);
  font: inherit;
  font-weight: 500;
}

.profile-dialog-input:focus,
.profile-dialog-select:focus {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--accent-bg);
}

.profile-dialog-error {
  margin: 0;
  color: var(--danger);
  font-size: 12px;
}

.profile-dialog-action {
  display: inline-grid;
  width: 40px;
  min-width: 40px;
  height: 40px;
  padding: 0;
  place-items: center;
  color: var(--text-h);
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--surface);
  cursor: pointer;
}

.profile-dialog-action .material-symbols-outlined {
  font-size: 20px;
}

.profile-dialog-action.primary {
  color: var(--on-accent);
  border-color: var(--accent);
  background: var(--accent);
}

.profile-dialog-action.remove {
  margin-right: auto;
  color: var(--danger);
  border-color: color-mix(in srgb, var(--danger) 35%, var(--border));
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

      <p v-if="profileError" class="profile-notice" role="alert">
        {{ profileError }}
      </p>

      <dl class="profile-details">
        <div class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">id_card</span>
            {{ t('app.accountInformation.fields.fullName') }}
          </dt>
          <dd class="profile-row-content">
            <span class="profile-row-value">{{ shownFullName }}</span>
            <button
              type="button"
              class="app-button profile-row-edit-button"
              :disabled="!profile || isLoadingProfile || isSavingProfile"
              :aria-label="t('app.accountInformation.profile.editInformation', { type: t('app.accountInformation.fields.fullName') })"
              :title="t('app.accountInformation.profile.editInformation', { type: t('app.accountInformation.fields.fullName') })"
              @click="openProfileDialog('fullName')"
            >
              <span class="material-symbols-outlined" aria-hidden="true">edit</span>
            </button>
          </dd>
        </div>

        <div class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">alternate_email</span>
            {{ t('app.accountInformation.fields.nickname') }}
          </dt>
          <dd class="profile-row-content">
            <span class="profile-row-value">{{ shownNickname }}</span>
            <button
              type="button"
              class="app-button profile-row-edit-button"
              :disabled="!profile || isLoadingProfile || isSavingProfile"
              :aria-label="t('app.accountInformation.profile.editInformation', { type: t('app.accountInformation.fields.nickname') })"
              :title="t('app.accountInformation.profile.editInformation', { type: t('app.accountInformation.fields.nickname') })"
              @click="openProfileDialog('nickname')"
            >
              <span class="material-symbols-outlined" aria-hidden="true">edit</span>
            </button>
          </dd>
        </div>

        <div
          v-for="claim in profile?.claims ?? []"
          :key="claim.name"
          class="profile-field"
        >
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">person_edit</span>
            {{ claimLabel(claim.name) }}
          </dt>
          <dd class="profile-row-content">
            <span class="profile-row-value profile-claim-value">{{ claim.value }}</span>
            <button
              type="button"
              class="app-button profile-row-edit-button"
              :disabled="isSavingProfile"
              :aria-label="t('app.accountInformation.profile.editInformation', { type: claimLabel(claim.name) })"
              :title="t('app.accountInformation.profile.editInformation', { type: claimLabel(claim.name) })"
              @click="openProfileDialog('claim', claim)"
            >
              <span class="material-symbols-outlined" aria-hidden="true">edit</span>
            </button>
          </dd>
        </div>

        <div v-if="availableClaimTypes.length > 0" class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">add_circle</span>
            {{ t('app.accountInformation.profile.additionalInformation') }}
          </dt>
          <dd>
            <button
              type="button"
              class="app-button profile-add-row-button"
              :disabled="!profile || isLoadingProfile || isSavingProfile"
              @click="openProfileDialog('add')"
            >
              <span class="material-symbols-outlined" aria-hidden="true">add</span>
              {{ t('app.accountInformation.profile.addInformation') }}
            </button>
          </dd>
        </div>

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

    <Dialog
      :is-open="isProfileDialogOpen"
      :title="profileDialogTitle"
      size="small"
      :close-on-backdrop="!isSavingProfile"
      :close-on-escape="!isSavingProfile"
      :show-close-button="!isSavingProfile"
      @update:is-open="updateProfileDialogOpen"
    >
      <p class="profile-dialog-description">
        {{ profileDialogMode === 'add'
          ? t('app.accountInformation.profile.addDialogDescription')
          : t('app.accountInformation.profile.editDialogDescription') }}
      </p>
      <form
        id="account-profile-dialog-form"
        class="profile-dialog-form"
        @submit.prevent="saveProfileDialogAsync"
      >
        <label v-if="profileDialogMode === 'add'" class="profile-dialog-field">
          <span>{{ t('app.accountInformation.profile.informationType') }}</span>
          <select
            v-model="profileDialogClaimName"
            class="profile-dialog-select"
            :disabled="isSavingProfile"
          >
            <option v-for="claimType in availableClaimTypes" :key="claimType" :value="claimType">
              {{ claimLabel(claimType) }}
            </option>
          </select>
        </label>
        <label class="profile-dialog-field">
          <span>{{ profileDialogFieldLabel }}</span>
          <input
            v-model="profileDialogValue"
            class="profile-dialog-input"
            type="text"
            :maxlength="profileDialogMaxLength"
            :required="profileDialogMode !== 'nickname'"
            :autocomplete="profileDialogMode === 'fullName'
              ? 'name'
              : profileDialogMode === 'nickname' ? 'nickname' : 'off'"
            :placeholder="profileDialogMode === 'nickname'
              ? t('app.accountInformation.profile.optionalPlaceholder')
              : t('app.accountInformation.profile.valuePlaceholder')"
            :disabled="isSavingProfile"
          />
        </label>
        <p v-if="profileDialogError" class="profile-dialog-error" role="alert">
          {{ profileDialogError }}
        </p>
      </form>

      <template #footer>
        <button
          v-if="profileDialogMode === 'claim'"
          type="button"
          class="app-button profile-dialog-action remove"
          :disabled="isSavingProfile"
          :aria-label="t('app.accountInformation.profile.removeInformation', { type: profileDialogFieldLabel })"
          :title="t('app.accountInformation.profile.removeInformation', { type: profileDialogFieldLabel })"
          @click="removeProfileClaimAsync"
        >
          <span class="material-symbols-outlined" aria-hidden="true">delete</span>
        </button>
        <button
          type="button"
          class="app-button profile-dialog-action"
          :disabled="isSavingProfile"
          :aria-label="t('app.accountInformation.profile.cancelAction')"
          :title="t('app.accountInformation.profile.cancelAction')"
          @click="updateProfileDialogOpen(false)"
        >
          <span class="material-symbols-outlined" aria-hidden="true">close</span>
        </button>
        <button
          type="submit"
          form="account-profile-dialog-form"
          class="app-button profile-dialog-action primary"
          :disabled="isSavingProfile || !canSaveProfileDialog"
          :aria-label="isSavingProfile
            ? t('app.accountInformation.profile.savingAction')
            : t('app.accountInformation.profile.saveAction')"
          :title="isSavingProfile
            ? t('app.accountInformation.profile.savingAction')
            : t('app.accountInformation.profile.saveAction')"
        >
          <span class="material-symbols-outlined" aria-hidden="true">
            {{ isSavingProfile ? 'progress_activity' : 'save' }}
          </span>
        </button>
      </template>
    </Dialog>

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
