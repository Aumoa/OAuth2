<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import {
  OrganizationGroups,
  type OrganizationGroupSummary,
} from '../api/OrganizationGroups.ts';
import {
  Organizations,
  type OrganizationMemberPage,
  type OrganizationMemberSummary,
  type OrganizationSummary,
} from '../api/Organizations.ts';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

const { locale, t } = useI18n();
const route = useRoute();
const organization = ref<OrganizationSummary | null>(null);
const group = ref<OrganizationGroupSummary | null>(null);
const isLoading = ref(true);
const loadFailed = ref(false);
const notFound = ref(false);
const members = ref<OrganizationMemberPage | null>(null);
const membersLoading = ref(false);
const membersLoadFailed = ref(false);
const memberPage = ref(1);
const memberPageSize = 10;
const addAccountId = ref('');
const isAddingMember = ref(false);
const addMemberError = ref<string | null>(null);
const memberActionId = ref<string | null>(null);
const memberActionError = ref<string | null>(null);
let isMounted = true;

const organizationId = computed(() => {
  const value = route.params.organizationId;
  return Array.isArray(value) ? value.join('/') : value ?? '';
});
const groupId = computed(() => {
  const value = route.params.groupId;
  return Array.isArray(value) ? value.join('/') : value ?? '';
});
const canManage = computed(() => (
  organization.value?.role === 'owner' || organization.value?.role === 'admin'
));
const totalMemberPages = computed(() => Math.max(
  1,
  Math.ceil((members.value?.totalCount ?? 0) / memberPageSize),
));
const createdAt = computed(() => {
  if (group.value === null) {
    return '';
  }

  return new Intl.DateTimeFormat(locale.value, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(group.value.createdAt));
});

function formatJoinedAt(value: string): string {
  return new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium' })
    .format(new Date(value));
}

async function loadMembersAsync(): Promise<void> {
  membersLoading.value = true;
  membersLoadFailed.value = false;
  try {
    const result = await OrganizationGroups.getMembersAsync(
      organizationId.value,
      groupId.value,
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

async function loadAsync(): Promise<void> {
  isLoading.value = true;
  loadFailed.value = false;
  notFound.value = false;
  organization.value = null;
  group.value = null;
  members.value = null;
  memberPage.value = 1;

  try {
    const [organizationResult, groupResult] = await Promise.all([
      Organizations.getAsync(organizationId.value),
      OrganizationGroups.getAsync(organizationId.value, groupId.value),
    ]);
    if (isMounted) {
      organization.value = organizationResult;
      group.value = groupResult;
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

async function movePageAsync(page: number): Promise<void> {
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
    await OrganizationGroups.addMemberAsync(
      organizationId.value,
      groupId.value,
      accountId,
    );
    addAccountId.value = '';
    memberPage.value = 1;
    await loadMembersAsync();
  } catch (error) {
    if (error instanceof HttpStatusCodeError && error.status === 404) {
      addMemberError.value = t('app.organizationManagement.groups.detail.errors.memberNotFound');
    } else if (error instanceof HttpStatusCodeError && error.status === 409) {
      addMemberError.value = t('app.organizationManagement.groups.detail.errors.alreadyMember');
    } else if (error instanceof HttpStatusCodeError && error.status === 403) {
      addMemberError.value = t('app.organizationManagement.groups.detail.errors.forbidden');
    } else {
      addMemberError.value = t('app.organizationManagement.groups.detail.errors.addFailed');
    }
  } finally {
    isAddingMember.value = false;
  }
}

async function deleteMemberAsync(member: OrganizationMemberSummary): Promise<void> {
  if (memberActionId.value !== null
    || !window.confirm(t('app.organizationManagement.groups.detail.confirmRemove', {
      account: member.accountId,
    }))) {
    return;
  }

  memberActionId.value = member.accountId;
  memberActionError.value = null;
  try {
    await OrganizationGroups.deleteMemberAsync(
      organizationId.value,
      groupId.value,
      member.accountId,
    );
    if (members.value?.items.length === 1 && memberPage.value > 1) {
      memberPage.value -= 1;
    }
    await loadMembersAsync();
  } catch {
    memberActionError.value = t('app.organizationManagement.groups.detail.errors.removeFailed');
  } finally {
    memberActionId.value = null;
  }
}

watch([organizationId, groupId], loadAsync);
onMounted(loadAsync);
onBeforeUnmount(() => {
  isMounted = false;
});
</script>

<style scoped lang="css">
.group-page {
  --organization-accent: #a78bfa;
  --organization-accent-strong: #8b5cf6;

  width: min(100%, 960px);
  margin: 0;
  padding: 16px 0 40px;
  box-sizing: border-box;
  text-align: left;
}

.group-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 20px;
}

.group-title {
  margin: 0;
  color: color-mix(in srgb, var(--organization-accent) 72%, var(--text-h));
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
  letter-spacing: -0.4px;
}

.group-description,
.section-description {
  margin: 7px 0 0;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.back-link {
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

.back-link:hover {
  border-color: color-mix(in srgb, var(--organization-accent) 68%, var(--border));
}

.information-panel,
.members-panel,
.group-state {
  box-sizing: border-box;
  border: 1px solid color-mix(in srgb, var(--organization-accent) 28%, var(--border));
  border-radius: 12px;
  background: color-mix(in srgb, var(--organization-accent-strong) 5%, var(--surface));
  box-shadow: var(--shadow-sm);
}

.information-panel,
.group-state {
  width: min(100%, 680px);
  padding: 22px;
}

.members-panel {
  width: 100%;
  margin-top: 20px;
  padding: 22px;
}

.section-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
}

.section-title,
.state-title {
  margin: 0;
  color: var(--text-h);
  font-size: 16px;
  font-weight: 750;
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

.group-fields {
  display: grid;
  margin: 14px 0 0;
}

.group-field {
  display: grid;
  grid-template-columns: 148px minmax(0, 1fr);
  gap: 16px;
  padding: 13px 0;
  border-top: 1px solid color-mix(in srgb, var(--organization-accent) 18%, var(--border));
}

.group-field dt {
  color: var(--text-muted);
  font-size: 12px;
  font-weight: 700;
}

.group-field dd {
  min-width: 0;
  margin: 0;
  overflow-wrap: anywhere;
  color: var(--text);
  font-size: 13px;
}

.group-id {
  font-family: var(--mono);
}

.add-member-form {
  display: flex;
  flex-wrap: wrap;
  gap: 9px;
  margin-top: 18px;
}

.member-input {
  flex: 1 1 240px;
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

.member-input:focus {
  border-color: var(--organization-accent);
  outline: 2px solid color-mix(in srgb, var(--organization-accent) 24%, transparent);
}

.add-member-form .app-button {
  width: max-content;
  min-width: 96px;
  padding: 0 14px;
  white-space: nowrap;
}

.feedback {
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
  grid-template-columns: minmax(180px, 1.5fr) minmax(150px, 1fr) 110px 90px;
  min-width: 0;
  padding: 13px 14px;
  align-items: center;
  gap: 12px;
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

.role-badge {
  display: inline-flex;
  width: fit-content;
  padding: 3px 8px;
  border-radius: 999px;
  color: color-mix(in srgb, var(--organization-accent) 85%, var(--text-h));
  background: color-mix(in srgb, var(--organization-accent-strong) 13%, transparent);
  font-size: 11px;
  font-weight: 800;
}

.remove-button {
  min-height: 34px;
  border-color: color-mix(in srgb, var(--danger) 35%, var(--border));
  color: var(--danger);
  background: color-mix(in srgb, var(--danger-bg) 45%, transparent);
  font-size: 12px;
  white-space: nowrap;
}

.pagination {
  display: flex;
  margin-top: 14px;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
}

.pagination span {
  color: var(--text-muted);
  font-size: 12px;
}

.pagination button {
  min-width: 72px;
  min-height: 34px;
  font-size: 12px;
}

.group-state {
  color: var(--text-muted);
  font-size: 13px;
}

.state-description {
  margin: 5px 0 0;
  line-height: 1.5;
}

@media (max-width: 640px) {
  .group-page {
    padding-top: 8px;
  }

  .group-header {
    flex-direction: column;
  }

  .group-field,
  .member-row {
    grid-template-columns: 1fr;
  }

  .remove-button,
  .add-member-form .app-button {
    width: 100%;
  }
}
</style>

<template>
  <section class="group-page" aria-labelledby="organization-group-title">
    <header class="group-header">
      <div>
        <h1 id="organization-group-title" class="group-title">
          {{ group?.name ?? t('app.organizationManagement.groups.detail.title') }}
        </h1>
        <p class="group-description">
          {{ t('app.organizationManagement.groups.detail.description', {
            organization: organization?.name ?? organizationId,
          }) }}
        </p>
      </div>
      <RouterLink
        class="back-link"
        :to="{ name: 'organization-management', params: { organizationId } }"
      >
        <span class="material-symbols-outlined" aria-hidden="true">arrow_back</span>
        {{ t('app.organizationManagement.groups.detail.back') }}
      </RouterLink>
    </header>

    <div v-if="isLoading" class="group-state" role="status">
      {{ t('app.organizationManagement.groups.detail.loading') }}
    </div>
    <div v-else-if="loadFailed" class="group-state" role="alert">
      <h2 class="state-title">{{ t('app.organizationManagement.groups.detail.loadFailed') }}</h2>
      <p class="state-description">
        {{ t('app.organizationManagement.loadFailedDescription') }}
      </p>
    </div>
    <div v-else-if="notFound" class="group-state">
      <h2 class="state-title">{{ t('app.organizationManagement.groups.detail.notFound') }}</h2>
      <p class="state-description">
        {{ t('app.organizationManagement.groups.detail.notFoundDescription') }}
      </p>
    </div>

    <section v-else-if="group" class="information-panel">
      <h2 class="section-title">
        {{ t('app.organizationManagement.groups.detail.basicInformation') }}
      </h2>
      <dl class="group-fields">
        <div class="group-field">
          <dt>{{ t('app.organizationManagement.groups.name') }}</dt>
          <dd>{{ group.name }}</dd>
        </div>
        <div class="group-field">
          <dt>{{ t('app.organizationManagement.groups.id') }}</dt>
          <dd class="group-id">{{ group.id }}</dd>
        </div>
        <div class="group-field">
          <dt>{{ t('app.organizationManagement.createdAt') }}</dt>
          <dd>{{ createdAt }}</dd>
        </div>
      </dl>
    </section>

    <section v-if="group" class="members-panel" aria-labelledby="group-members-title">
      <div class="section-heading">
        <div>
          <h2 id="group-members-title" class="section-title">
            {{ t('app.organizationManagement.groups.detail.membersTitle') }}
          </h2>
          <p class="section-description">
            {{ t('app.organizationManagement.groups.detail.membersDescription') }}
          </p>
        </div>
        <span class="member-count">{{ members?.totalCount ?? group.memberCount }}</span>
      </div>

      <form v-if="canManage" class="add-member-form" @submit.prevent="addMemberAsync">
        <input
          v-model="addAccountId"
          class="member-input"
          type="text"
          maxlength="128"
          autocomplete="off"
          :placeholder="t('app.organizationManagement.groups.detail.accountIdPlaceholder')"
          :disabled="isAddingMember"
        />
        <button
          type="submit"
          class="app-button"
          :disabled="isAddingMember || !addAccountId.trim()"
        >
          {{ t('app.organizationManagement.groups.detail.add') }}
        </button>
      </form>
      <p v-if="addMemberError" class="feedback" role="alert">{{ addMemberError }}</p>
      <p v-if="memberActionError" class="feedback" role="alert">{{ memberActionError }}</p>

      <div v-if="membersLoading" class="members-state" role="status">
        {{ t('app.organizationManagement.groups.detail.membersLoading') }}
      </div>
      <div v-else-if="membersLoadFailed" class="members-state" role="alert">
        {{ t('app.organizationManagement.groups.detail.membersLoadFailed') }}
      </div>
      <div v-else-if="members && members.items.length === 0" class="members-state">
        {{ t('app.organizationManagement.groups.detail.membersEmpty') }}
      </div>
      <div v-else-if="members" class="member-list">
        <article v-for="member in members.items" :key="member.accountId" class="member-row">
          <div class="member-identity">
            <span class="member-name">{{ member.name }}</span>
            <span class="member-account">{{ member.accountId }}</span>
          </div>
          <div class="member-contact">
            <span class="member-joined">
              {{ t('app.organizationManagement.groups.detail.joinedAt', {
                date: formatJoinedAt(member.joinedAt),
              }) }}
            </span>
          </div>
          <span class="role-badge">
            {{ t(`app.organizationManagement.roles.${member.role}`) }}
          </span>
          <button
            v-if="canManage"
            type="button"
            class="app-button remove-button"
            :disabled="memberActionId !== null"
            @click="deleteMemberAsync(member)"
          >
            {{ t('app.organizationManagement.groups.detail.remove') }}
          </button>
          <span v-else aria-hidden="true"></span>
        </article>
      </div>

      <nav v-if="members && totalMemberPages > 1" class="pagination" aria-label="Members">
        <button
          type="button"
          class="app-button"
          :disabled="membersLoading || memberPage <= 1"
          @click="movePageAsync(memberPage - 1)"
        >
          {{ t('app.organizationManagement.members.previous') }}
        </button>
        <span>{{ memberPage }} / {{ totalMemberPages }}</span>
        <button
          type="button"
          class="app-button"
          :disabled="membersLoading || memberPage >= totalMemberPages"
          @click="movePageAsync(memberPage + 1)"
        >
          {{ t('app.organizationManagement.members.next') }}
        </button>
      </nav>
    </section>
  </section>
</template>
