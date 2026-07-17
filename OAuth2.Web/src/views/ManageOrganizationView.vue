<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import {
  Organizations,
  type OrganizationMemberPage,
  type OrganizationMemberSummary,
  type OrganizationRole,
  type OrganizationSummary,
} from '../api/Organizations.ts';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';
import { useOrganizationsStore } from '../stores/organizations.ts';

const { locale, t } = useI18n();
const route = useRoute();
const router = useRouter();
const organizationsStore = useOrganizationsStore();
const organization = ref<OrganizationSummary | null>(null);
const isLoading = ref(true);
const loadFailed = ref(false);
const notFound = ref(false);
const members = ref<OrganizationMemberPage | null>(null);
const membersLoading = ref(false);
const membersLoadFailed = ref(false);
const memberPage = ref(1);
const memberPageSize = 10;
const addAccountId = ref('');
const addRole = ref<OrganizationRole>('member');
const isAddingMember = ref(false);
const addMemberError = ref<string | null>(null);
const memberActionId = ref<string | null>(null);
const memberActionError = ref<string | null>(null);
const transferAccountId = ref('');
const isTransferringOwnership = ref(false);
const transferError = ref<string | null>(null);
const deleteConfirmation = ref('');
const isDeletingOrganization = ref(false);
const deleteOrganizationError = ref<string | null>(null);
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
const totalMemberPages = computed(() => Math.max(
  1,
  Math.ceil((members.value?.totalCount ?? 0) / memberPageSize),
));
const canManageMembers = computed(() => (
  organization.value?.role === 'owner' || organization.value?.role === 'admin'
));
const addableRoles = computed<OrganizationRole[]>(() => (
  organization.value?.role === 'owner' ? ['admin', 'member'] : ['member']
));
const isOwner = computed(() => organization.value?.role === 'owner');
const deleteConfirmationMatches = computed(() => (
  organization.value !== null
  && deleteConfirmation.value === organization.value.name
));

function roleRank(role: OrganizationRole): number {
  switch (role) {
    case 'owner':
      return 3;
    case 'admin':
      return 2;
    case 'member':
      return 1;
  }
}

function canManageMember(member: OrganizationMemberSummary): boolean {
  if (organization.value === null) {
    return false;
  }

  return roleRank(organization.value.role) > roleRank(member.role);
}

function assignableRoles(member: OrganizationMemberSummary): OrganizationRole[] {
  if (!canManageMember(member)) {
    return [member.role];
  }

  return ['admin', 'member'];
}

function formatJoinedAt(value: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    dateStyle: 'medium',
  }).format(new Date(value));
}

async function loadMembersAsync(): Promise<void> {
  membersLoading.value = true;
  membersLoadFailed.value = false;
  try {
    const result = await Organizations.getMembersAsync(
      organizationId.value,
      memberPage.value,
      memberPageSize,
    );
    if (isMounted) {
      members.value = result;
    }
  } catch {
    if (isMounted) {
      membersLoadFailed.value = true;
    }
  } finally {
    if (isMounted) {
      membersLoading.value = false;
    }
  }
}

async function loadOrganizationAsync(): Promise<void> {
  isLoading.value = true;
  loadFailed.value = false;
  notFound.value = false;
  organization.value = null;
  addRole.value = 'member';
  deleteConfirmation.value = '';
  deleteOrganizationError.value = null;

  try {
    const result = await Organizations.getAsync(organizationId.value);
    if (isMounted) {
      organization.value = result;
      memberPage.value = 1;
      await loadMembersAsync();
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

async function moveMemberPageAsync(page: number): Promise<void> {
  if (membersLoading.value || page < 1 || page > totalMemberPages.value) {
    return;
  }

  memberPage.value = page;
  memberActionError.value = null;
  await loadMembersAsync();
}

async function addMemberAsync(): Promise<void> {
  const accountId = addAccountId.value.trim();
  if (!accountId || isAddingMember.value) {
    return;
  }

  isAddingMember.value = true;
  addMemberError.value = null;
  try {
    await Organizations.addMemberAsync(organizationId.value, accountId, addRole.value);
    addAccountId.value = '';
    addRole.value = 'member';
    memberPage.value = 1;
    await loadMembersAsync();
  } catch (error) {
    if (error instanceof HttpStatusCodeError && error.status === 404) {
      addMemberError.value = t('app.organizationManagement.members.errors.accountNotFound');
    } else if (error instanceof HttpStatusCodeError && error.status === 409) {
      addMemberError.value = t('app.organizationManagement.members.errors.alreadyMember');
    } else if (error instanceof HttpStatusCodeError && error.status === 403) {
      addMemberError.value = t('app.organizationManagement.members.errors.forbidden');
    } else {
      addMemberError.value = t('app.organizationManagement.members.errors.addFailed');
    }
  } finally {
    isAddingMember.value = false;
  }
}

async function updateMemberRoleAsync(
  member: OrganizationMemberSummary,
  event: Event,
): Promise<void> {
  const role = (event.target as HTMLSelectElement).value as OrganizationRole;
  if (role === member.role || memberActionId.value !== null) {
    return;
  }

  memberActionId.value = member.accountId;
  memberActionError.value = null;
  try {
    await Organizations.updateMemberAsync(organizationId.value, member.accountId, role);
    await loadMembersAsync();
  } catch {
    await loadMembersAsync();
    memberActionError.value = t('app.organizationManagement.members.errors.updateFailed');
  } finally {
    memberActionId.value = null;
  }
}

async function deleteMemberAsync(member: OrganizationMemberSummary): Promise<void> {
  if (memberActionId.value !== null
    || !window.confirm(t('app.organizationManagement.members.confirmRemove', {
      account: member.accountId,
    }))) {
    return;
  }

  memberActionId.value = member.accountId;
  memberActionError.value = null;
  try {
    await Organizations.deleteMemberAsync(organizationId.value, member.accountId);
    if (members.value?.items.length === 1 && memberPage.value > 1) {
      memberPage.value -= 1;
    }
    await loadMembersAsync();
  } catch {
    memberActionError.value = t('app.organizationManagement.members.errors.removeFailed');
  } finally {
    memberActionId.value = null;
  }
}

async function transferOwnershipAsync(): Promise<void> {
  const accountId = transferAccountId.value.trim();
  if (!accountId
    || isTransferringOwnership.value
    || !window.confirm(t('app.organizationManagement.ownerTransfer.confirm', { account: accountId }))) {
    return;
  }

  isTransferringOwnership.value = true;
  transferError.value = null;
  try {
    await Organizations.transferOwnershipAsync(organizationId.value, accountId);
    transferAccountId.value = '';
    await loadOrganizationAsync();
  } catch (error) {
    transferError.value = error instanceof HttpStatusCodeError && error.status === 404
      ? t('app.organizationManagement.ownerTransfer.memberNotFound')
      : t('app.organizationManagement.ownerTransfer.failed');
  } finally {
    isTransferringOwnership.value = false;
  }
}

async function deleteOrganizationAsync(): Promise<void> {
  if (organization.value === null
    || !deleteConfirmationMatches.value
    || isDeletingOrganization.value
    || !window.confirm(t('app.organizationManagement.organizationDelete.confirm', {
      name: organization.value.name,
    }))) {
    return;
  }

  isDeletingOrganization.value = true;
  deleteOrganizationError.value = null;
  try {
    const deletedOrganizationId = organization.value.id;
    await Organizations.deleteAsync(deletedOrganizationId, deleteConfirmation.value);
    organizationsStore.remove(deletedOrganizationId);
    await router.push('/');
  } catch (error) {
    if (error instanceof HttpStatusCodeError && error.status === 403) {
      deleteOrganizationError.value = t(
        'app.organizationManagement.organizationDelete.ownerRequired',
      );
    } else if (error instanceof HttpStatusCodeError && error.status === 409) {
      deleteOrganizationError.value = t(
        'app.organizationManagement.organizationDelete.nameMismatch',
      );
    } else if (error instanceof HttpStatusCodeError && error.status === 422) {
      deleteOrganizationError.value = t(
        'app.organizationManagement.organizationDelete.ownerConflict',
      );
    } else {
      deleteOrganizationError.value = t(
        'app.organizationManagement.organizationDelete.failed',
      );
    }
  } finally {
    isDeletingOrganization.value = false;
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

.members-panel,
.organization-danger-zone {
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

.section-description {
  margin: 5px 0 0;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.member-count {
  flex: 0 0 auto;
  padding: 3px 9px;
  border-radius: 999px;
  color: color-mix(in srgb, var(--organization-accent) 85%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 13%, transparent);
  font-size: 12px;
  font-weight: 800;
}

.add-member-form {
  display: flex;
  flex-wrap: wrap;
  gap: 9px;
  margin-top: 18px;
}

.add-member-form .member-input {
  flex: 1 1 220px;
}

.add-member-form .member-role-select {
  flex: 1 1 130px;
  max-width: 180px;
}

.member-input,
.member-role-select {
  min-width: 0;
  min-height: 38px;
  padding: 0 11px;
  box-sizing: border-box;
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text);
  background: var(--surface);
  font: inherit;
  font-size: 13px;
}

.member-input:focus,
.member-role-select:focus {
  border-color: var(--organization-accent);
  outline: 2px solid color-mix(in srgb, var(--organization-accent) 24%, transparent);
}

.member-feedback {
  margin: 9px 0 0;
  color: var(--danger);
  font-size: 12px;
}

.members-state {
  margin-top: 18px;
  padding: 16px;
  border: 1px dashed color-mix(in srgb, var(--organization-accent) 22%, var(--border));
  border-radius: 9px;
  color: var(--text-muted);
  font-size: 13px;
}

.member-list {
  display: grid;
  margin-top: 18px;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 20%, var(--border));
  border-radius: 10px;
  overflow: hidden;
}

.member-row {
  display: grid;
  grid-template-columns: minmax(180px, 1.5fr) minmax(160px, 1fr) 132px 90px;
  min-width: 0;
  align-items: center;
  gap: 12px;
  padding: 13px 14px;
  background: color-mix(in srgb, var(--surface) 97%, transparent);
}

.member-row + .member-row {
  border-top: 1px solid color-mix(in srgb, var(--organization-accent) 16%, var(--border));
}

.member-identity,
.member-contact {
  min-width: 0;
}

.member-name,
.member-account,
.member-joined {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.member-name {
  color: var(--text-h);
  font-size: 13px;
  font-weight: 750;
}

.member-account,
.member-joined {
  color: var(--text-muted);
  font-size: 11px;
}

.member-account {
  margin-top: 2px;
  font-family: var(--mono);
}

.member-role-select {
  width: 100%;
}

.member-role-badge {
  display: inline-flex;
  width: fit-content;
  padding: 3px 8px;
  border-radius: 999px;
  color: color-mix(in srgb, var(--organization-accent) 85%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 13%, transparent);
  font-size: 11px;
  font-weight: 800;
}

.remove-member-button {
  min-height: 34px;
  border-color: color-mix(in srgb, var(--danger) 35%, var(--border));
  color: var(--danger);
  background: color-mix(in srgb, var(--danger-bg) 45%, transparent);
  font-size: 12px;
}

.member-pagination {
  display: flex;
  margin-top: 14px;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
}

.member-pagination span {
  color: var(--text-muted);
  font-size: 12px;
}

.member-pagination button {
  min-width: 72px;
  min-height: 34px;
  font-size: 12px;
}

.organization-danger-zone {
  border-color: color-mix(in srgb, var(--danger) 52%, var(--border));
  background: color-mix(in srgb, var(--danger-bg) 26%, var(--surface));
}

.danger-zone-title {
  margin: 0;
  color: var(--danger);
  font-size: 17px;
  font-weight: 800;
}

.danger-action {
  margin-top: 18px;
}

.danger-action + .danger-action {
  margin-top: 22px;
  padding-top: 22px;
  border-top: 1px solid color-mix(in srgb, var(--danger) 24%, var(--border));
}

.danger-action-title {
  margin: 0;
  color: var(--text-h);
  font-size: 14px;
  font-weight: 750;
}

.owner-transfer-form {
  display: flex;
  flex-wrap: wrap;
  gap: 9px;
  margin-top: 15px;
}

.organization-delete-form {
  display: flex;
  flex-wrap: wrap;
  gap: 9px;
  margin-top: 15px;
}

.owner-transfer-form .member-input,
.organization-delete-form .member-input {
  flex: 1 1 240px;
}

.add-member-form .app-button,
.owner-transfer-button,
.organization-delete-button {
  width: max-content;
  min-width: 96px;
  padding: 0 14px;
  white-space: nowrap;
}

.organization-delete-input:focus {
  border-color: var(--danger);
  outline: 2px solid color-mix(in srgb, var(--danger) 22%, transparent);
}

.owner-transfer-button {
  border-color: color-mix(in srgb, var(--danger) 55%, var(--border));
  color: var(--danger);
  background: color-mix(in srgb, var(--danger-bg) 55%, transparent);
}

.organization-delete-button {
  border-color: color-mix(in srgb, var(--danger) 68%, var(--border));
  color: var(--danger);
  background: color-mix(in srgb, var(--danger-bg) 62%, transparent);
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

  .member-row {
    grid-template-columns: 1fr;
  }

  .add-member-form .member-role-select {
    max-width: none;
  }

  .member-row {
    gap: 8px;
  }

  .remove-member-button {
    width: 100%;
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

    <section v-if="organization" class="members-panel" aria-labelledby="organization-members-title">
      <div class="section-heading">
        <div>
          <h2 id="organization-members-title" class="organization-section-title">
            {{ t('app.organizationManagement.members.title') }}
          </h2>
          <p class="section-description">
            {{ t('app.organizationManagement.members.description') }}
          </p>
        </div>
        <span class="member-count">
          {{ members?.totalCount ?? 0 }}
        </span>
      </div>

      <form v-if="canManageMembers" class="add-member-form" @submit.prevent="addMemberAsync">
        <input
          v-model="addAccountId"
          class="member-input"
          type="text"
          maxlength="128"
          autocomplete="off"
          :placeholder="t('app.organizationManagement.members.accountIdPlaceholder')"
          :aria-label="t('app.organizationManagement.members.accountId')"
          :disabled="isAddingMember"
        />
        <select v-model="addRole" class="member-role-select" :disabled="isAddingMember">
          <option v-for="role in addableRoles" :key="role" :value="role">
            {{ t(`app.organizationManagement.roles.${role}`) }}
          </option>
        </select>
        <button
          type="submit"
          class="app-button"
          :disabled="isAddingMember || !addAccountId.trim()"
        >
          {{ t('app.organizationManagement.members.add') }}
        </button>
      </form>
      <p v-if="addMemberError" class="member-feedback" role="alert">
        {{ addMemberError }}
      </p>
      <p v-if="memberActionError" class="member-feedback" role="alert">
        {{ memberActionError }}
      </p>

      <div v-if="membersLoading" class="members-state" role="status">
        {{ t('app.organizationManagement.members.loading') }}
      </div>
      <div v-else-if="membersLoadFailed" class="members-state" role="alert">
        {{ t('app.organizationManagement.members.loadFailed') }}
      </div>
      <div v-else-if="members && members.items.length === 0" class="members-state">
        {{ t('app.organizationManagement.members.empty') }}
      </div>
      <div v-else-if="members" class="member-list">
        <article v-for="member in members.items" :key="member.accountId" class="member-row">
          <div class="member-identity">
            <span class="member-name">{{ member.name }}</span>
            <span class="member-account">{{ member.accountId }}</span>
          </div>
          <div class="member-contact">
            <span class="member-joined">
              {{ t('app.organizationManagement.members.joinedAt', {
                date: formatJoinedAt(member.joinedAt),
              }) }}
            </span>
          </div>
          <select
            v-if="canManageMember(member)"
            class="member-role-select"
            :value="member.role"
            :disabled="memberActionId !== null"
            @change="updateMemberRoleAsync(member, $event)"
          >
            <option v-for="role in assignableRoles(member)" :key="role" :value="role">
              {{ t(`app.organizationManagement.roles.${role}`) }}
            </option>
          </select>
          <span v-else class="member-role-badge">
            {{ t(`app.organizationManagement.roles.${member.role}`) }}
          </span>
          <button
            v-if="canManageMember(member)"
            type="button"
            class="app-button remove-member-button"
            :disabled="memberActionId !== null"
            @click="deleteMemberAsync(member)"
          >
            {{ t('app.organizationManagement.members.remove') }}
          </button>
          <span v-else aria-hidden="true"></span>
        </article>
      </div>

      <nav v-if="members && totalMemberPages > 1" class="member-pagination" aria-label="Members">
        <button
          type="button"
          class="app-button"
          :disabled="membersLoading || memberPage <= 1"
          @click="moveMemberPageAsync(memberPage - 1)"
        >
          {{ t('app.organizationManagement.members.previous') }}
        </button>
        <span>{{ memberPage }} / {{ totalMemberPages }}</span>
        <button
          type="button"
          class="app-button"
          :disabled="membersLoading || memberPage >= totalMemberPages"
          @click="moveMemberPageAsync(memberPage + 1)"
        >
          {{ t('app.organizationManagement.members.next') }}
        </button>
      </nav>
    </section>

    <section
      v-if="organization && isOwner"
      class="organization-danger-zone"
      aria-labelledby="organization-danger-zone-title"
    >
      <h2 id="organization-danger-zone-title" class="danger-zone-title">
        {{ t('app.organizationManagement.dangerZone') }}
      </h2>

      <div class="danger-action">
        <h3 class="danger-action-title">
          {{ t('app.organizationManagement.ownerTransfer.title') }}
        </h3>
        <p class="section-description">
          {{ t('app.organizationManagement.ownerTransfer.description') }}
        </p>
        <form class="owner-transfer-form" @submit.prevent="transferOwnershipAsync">
          <input
            v-model="transferAccountId"
            class="member-input"
            type="text"
            maxlength="128"
            autocomplete="off"
            :placeholder="t('app.organizationManagement.ownerTransfer.placeholder')"
            :disabled="isTransferringOwnership"
          />
          <button
            type="submit"
            class="app-button owner-transfer-button"
            :disabled="isTransferringOwnership || !transferAccountId.trim()"
          >
            {{ t('app.organizationManagement.ownerTransfer.submit') }}
          </button>
        </form>
        <p v-if="transferError" class="member-feedback" role="alert">
          {{ transferError }}
        </p>
      </div>

      <div class="danger-action">
        <h3 class="danger-action-title">
          {{ t('app.organizationManagement.organizationDelete.title') }}
        </h3>
        <p class="section-description">
          {{ t('app.organizationManagement.organizationDelete.description') }}
        </p>
        <form class="organization-delete-form" @submit.prevent="deleteOrganizationAsync">
          <input
            v-model="deleteConfirmation"
            class="member-input organization-delete-input"
            type="text"
            maxlength="128"
            autocomplete="off"
            :placeholder="t('app.organizationManagement.organizationDelete.placeholder', {
              name: organization.name,
            })"
            :aria-label="t('app.organizationManagement.organizationDelete.confirmationLabel')"
            :disabled="isDeletingOrganization"
          />
          <button
            type="submit"
            class="app-button organization-delete-button"
            :disabled="isDeletingOrganization || !deleteConfirmationMatches"
          >
            {{ t('app.organizationManagement.organizationDelete.submit') }}
          </button>
        </form>
        <p v-if="deleteOrganizationError" class="member-feedback" role="alert">
          {{ deleteOrganizationError }}
        </p>
      </div>
    </section>
  </section>
</template>
