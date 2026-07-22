<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  Accounts,
  type AccountProfile,
  type AccountProfileClaim,
} from '../api/accounts.ts';
import ProfileImageEditor from '../components/ProfileImageEditor.vue';
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

const profile = ref<AccountProfile | null>(null);
const isLoadingProfile = ref(false);
const isEditingProfile = ref(false);
const isSavingProfile = ref(false);
const profileError = ref<string | null>(null);
const editFullName = ref('');
const editNickname = ref('');
const editClaims = ref<AccountProfileClaim[]>([]);

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
const canAddClaim = computed(() => editClaims.value.length < claimTypes.length);
const isProfileFormValid = computed(() => (
  editFullName.value.trim().length > 0
  && editFullName.value.trim().length <= 128
  && editNickname.value.trim().length <= 128
  && editClaims.value.every((claim) => (
    claimTypes.includes(claim.name as ClaimType)
    && claim.value.trim().length > 0
    && claim.value.trim().length <= 2048
  ))
  && new Set(editClaims.value.map((claim) => claim.name)).size === editClaims.value.length
));

function claimLabel(name: string): string {
  return t(`app.accountInformation.claimTypes.${name}`);
}

function availableClaimTypes(index: number): readonly ClaimType[] {
  const selected = new Set(editClaims.value
    .filter((_, claimIndex) => claimIndex !== index)
    .map((claim) => claim.name));
  return claimTypes.filter((name) => !selected.has(name));
}

function beginProfileEdit(): void {
  if (!profile.value || isLoadingProfile.value) {
    return;
  }

  editFullName.value = profile.value.fullName;
  editNickname.value = profile.value.nickname ?? '';
  editClaims.value = profile.value.claims.map((claim) => ({ ...claim }));
  profileError.value = null;
  isEditingProfile.value = true;
}

function cancelProfileEdit(): void {
  if (!isSavingProfile.value) {
    isEditingProfile.value = false;
    profileError.value = null;
  }
}

function addClaim(): void {
  const nextType = availableClaimTypes(-1)[0];
  if (nextType) {
    editClaims.value.push({ name: nextType, value: '' });
  }
}

function removeClaim(index: number): void {
  editClaims.value.splice(index, 1);
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

async function saveProfileAsync(): Promise<void> {
  if (isSavingProfile.value || !isProfileFormValid.value) {
    return;
  }

  isSavingProfile.value = true;
  profileError.value = null;
  try {
    const saved = await Accounts.updateProfileAsync({
      fullName: editFullName.value.trim(),
      nickname: editNickname.value.trim() || null,
      claims: editClaims.value.map((claim) => ({
        name: claim.name,
        value: claim.value.trim(),
      })),
    });
    profile.value = saved;
    auth.setProfile(saved.fullName, saved.nickname ?? undefined);
    isEditingProfile.value = false;
  } catch {
    profileError.value = t('app.accountInformation.profile.saveFailed');
  } finally {
    isSavingProfile.value = false;
  }
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
  position: relative;
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
  padding-right: 150px;
}

.profile-edit-button {
  position: absolute;
  top: 22px;
  right: 24px;
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
}

.profile-edit-button:hover:not(:disabled) {
  background: var(--accent-bg);
}

.profile-edit-button:disabled {
  cursor: default;
  opacity: 0.55;
}

.profile-edit-button .material-symbols-outlined {
  font-size: 17px;
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

.profile-editor {
  padding: 20px 24px;
  border-bottom: 1px solid var(--border);
  background: color-mix(in srgb, var(--surface-muted) 45%, transparent);
}

.profile-editor-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.profile-editor-field {
  display: grid;
  gap: 6px;
  color: var(--text-muted);
  font-size: 12px;
  font-weight: 700;
}

.profile-editor-input,
.profile-claim-select,
.profile-claim-input {
  width: 100%;
  min-width: 0;
  height: 40px;
  padding: 0 11px;
  box-sizing: border-box;
  color: var(--text-h);
  border: 1px solid var(--border);
  border-radius: 8px;
  outline: none;
  background: var(--surface);
  font: inherit;
  font-weight: 500;
}

.profile-editor-input:focus,
.profile-claim-select:focus,
.profile-claim-input:focus {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--accent-bg);
}

.profile-claims-editor {
  margin-top: 18px;
}

.profile-claims-heading {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 9px;
}

.profile-claims-title {
  margin: 0;
  color: var(--text-h);
  font-size: 13px;
  font-weight: 700;
}

.profile-add-claim,
.profile-remove-claim,
.profile-editor-action {
  border-radius: 8px;
  cursor: pointer;
  font-weight: 700;
}

.profile-add-claim {
  padding: 6px 9px;
  color: var(--accent);
  border: 1px solid var(--accent-border);
  background: var(--surface);
  font-size: 12px;
}

.profile-add-claim:disabled,
.profile-remove-claim:disabled,
.profile-editor-action:disabled {
  cursor: default;
  opacity: 0.55;
}

.profile-claim-list {
  display: grid;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.profile-claim-row {
  display: grid;
  grid-template-columns: minmax(150px, 0.42fr) minmax(0, 1fr) auto;
  gap: 8px;
  align-items: center;
}

.profile-remove-claim {
  display: grid;
  width: 40px;
  height: 40px;
  padding: 0;
  place-items: center;
  color: var(--danger);
  border: 1px solid color-mix(in srgb, var(--danger) 35%, var(--border));
  background: var(--surface);
}

.profile-remove-claim .material-symbols-outlined {
  font-size: 19px;
}

.profile-empty-claims {
  margin: 8px 0 0;
  color: var(--text-muted);
  font-size: 12px;
}

.profile-editor-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 18px;
}

.profile-editor-action {
  padding: 8px 14px;
  border: 1px solid var(--border);
  background: var(--surface);
}

.profile-editor-action.primary {
  color: var(--on-accent);
  border-color: var(--accent);
  background: var(--accent);
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

  .profile-heading {
    padding-right: 0;
  }

  .profile-edit-button {
    position: static;
    margin-top: 10px;
  }

  .profile-editor {
    padding: 18px;
  }

  .profile-editor-grid,
  .profile-claim-row {
    grid-template-columns: 1fr;
  }

  .profile-remove-claim {
    width: 100%;
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
            v-if="!isEditingProfile"
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
          <button
            type="button"
            class="app-button profile-edit-button"
            :disabled="!profile || isLoadingProfile || isSavingProfile"
            @click="beginProfileEdit"
          >
            <span class="material-symbols-outlined" aria-hidden="true">edit</span>
            {{ t('app.accountInformation.profile.editAction') }}
          </button>
        </div>
      </div>

      <p v-if="profileError" class="profile-notice" role="alert">
        {{ profileError }}
      </p>

      <form v-if="isEditingProfile" class="profile-editor" @submit.prevent="saveProfileAsync">
        <div class="profile-editor-grid">
          <label class="profile-editor-field">
            <span>{{ t('app.accountInformation.fields.fullName') }}</span>
            <input
              v-model="editFullName"
              class="profile-editor-input"
              type="text"
              maxlength="128"
              autocomplete="name"
              required
              :disabled="isSavingProfile"
            />
          </label>
          <label class="profile-editor-field">
            <span>{{ t('app.accountInformation.fields.nickname') }}</span>
            <input
              v-model="editNickname"
              class="profile-editor-input"
              type="text"
              maxlength="128"
              autocomplete="nickname"
              :placeholder="t('app.accountInformation.profile.optionalPlaceholder')"
              :disabled="isSavingProfile"
            />
          </label>
        </div>

        <section class="profile-claims-editor" aria-labelledby="additional-profile-information">
          <div class="profile-claims-heading">
            <h3 id="additional-profile-information" class="profile-claims-title">
              {{ t('app.accountInformation.profile.additionalInformation') }}
            </h3>
            <button
              type="button"
              class="app-button profile-add-claim"
              :disabled="isSavingProfile || !canAddClaim"
              @click="addClaim"
            >
              {{ t('app.accountInformation.profile.addInformation') }}
            </button>
          </div>

          <ul v-if="editClaims.length > 0" class="profile-claim-list">
            <li v-for="(claim, index) in editClaims" :key="index" class="profile-claim-row">
              <select
                v-model="claim.name"
                class="profile-claim-select"
                :aria-label="t('app.accountInformation.profile.informationType')"
                :disabled="isSavingProfile"
              >
                <option
                  v-for="claimType in availableClaimTypes(index)"
                  :key="claimType"
                  :value="claimType"
                >
                  {{ claimLabel(claimType) }}
                </option>
              </select>
              <input
                v-model="claim.value"
                class="profile-claim-input"
                type="text"
                maxlength="2048"
                required
                :aria-label="t('app.accountInformation.profile.informationValue')"
                :placeholder="t('app.accountInformation.profile.valuePlaceholder')"
                :disabled="isSavingProfile"
              />
              <button
                type="button"
                class="app-button profile-remove-claim"
                :aria-label="t('app.accountInformation.profile.removeInformation', { type: claimLabel(claim.name) })"
                :title="t('app.accountInformation.profile.removeInformation', { type: claimLabel(claim.name) })"
                :disabled="isSavingProfile"
                @click="removeClaim(index)"
              >
                <span class="material-symbols-outlined" aria-hidden="true">delete</span>
              </button>
            </li>
          </ul>
          <p v-else class="profile-empty-claims">
            {{ t('app.accountInformation.profile.noAdditionalInformation') }}
          </p>
        </section>

        <div class="profile-editor-actions">
          <button
            type="button"
            class="app-button profile-editor-action"
            :disabled="isSavingProfile"
            @click="cancelProfileEdit"
          >
            {{ t('app.accountInformation.profile.cancelAction') }}
          </button>
          <button
            type="submit"
            class="app-button profile-editor-action primary"
            :disabled="isSavingProfile || !isProfileFormValid"
          >
            {{ isSavingProfile
              ? t('app.accountInformation.profile.savingAction')
              : t('app.accountInformation.profile.saveAction') }}
          </button>
        </div>
      </form>

      <dl class="profile-details">
        <div v-if="!isEditingProfile" class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">id_card</span>
            {{ t('app.accountInformation.fields.fullName') }}
          </dt>
          <dd>{{ shownFullName }}</dd>
        </div>

        <div v-if="!isEditingProfile" class="profile-field">
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">alternate_email</span>
            {{ t('app.accountInformation.fields.nickname') }}
          </dt>
          <dd>{{ shownNickname }}</dd>
        </div>

        <div
          v-for="claim in profile?.claims ?? []"
          v-show="!isEditingProfile"
          :key="claim.name"
          class="profile-field"
        >
          <dt>
            <span class="material-symbols-outlined" aria-hidden="true">person_edit</span>
            {{ claimLabel(claim.name) }}
          </dt>
          <dd class="profile-claim-value">{{ claim.value }}</dd>
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
