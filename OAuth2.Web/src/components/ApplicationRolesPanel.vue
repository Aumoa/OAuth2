<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  Applications,
  type ApplicationRoleMemberPage,
  type ApplicationRoleSummary,
} from '../api/applications.ts';
import Dialog from '../core/components/Dialog.vue';
import FloatingInput from '../core/components/FloatingInput.vue';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

const props = defineProps<{
  clientId: string;
  organizationId?: string;
  rolesScopeEnabled: boolean;
}>();

const pageSize = 20;
const { locale, t } = useI18n();
const roles = ref<ApplicationRoleSummary[]>([]);
const isLoading = ref(false);
const loadError = ref<string | null>(null);
const selectedRoleId = ref<string | null>(null);
const memberPage = ref<ApplicationRoleMemberPage | null>(null);
const areMembersLoading = ref(false);
const membersError = ref<string | null>(null);
const memberIdentifier = ref('');
const memberActionError = ref<string | null>(null);
const isAddingMember = ref(false);
const removingAccountId = ref<string | null>(null);
const isCreateDialogOpen = ref(false);
const roleId = ref('');
const roleName = ref('');
const roleIdError = ref<string | null>(null);
const roleNameError = ref<string | null>(null);
const createError = ref<string | null>(null);
const isCreating = ref(false);
const roleIdInput = ref<InstanceType<typeof FloatingInput> | null>(null);
const deletingRole = ref<ApplicationRoleSummary | null>(null);
const isDeleteDialogOpen = ref(false);
const deleteError = ref<string | null>(null);
const isDeleting = ref(false);
let rolesRequestId = 0;
let membersRequestId = 0;

const selectedRole = computed(() => (
  roles.value.find(role => role.id === selectedRoleId.value) ?? null
));
const totalPages = computed(() => Math.max(
  1,
  Math.ceil((memberPage.value?.totalCount ?? 0) / pageSize),
));

function formatAssignedAt(value: string): string {
  return new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium' }).format(new Date(value));
}

function sortRoles(): void {
  roles.value.sort((left, right) => (
    left.name.localeCompare(right.name, locale.value) || left.id.localeCompare(right.id)
  ));
}

async function loadRolesAsync(): Promise<void> {
  const requestId = ++rolesRequestId;
  ++membersRequestId;
  isLoading.value = true;
  loadError.value = null;
  selectedRoleId.value = null;
  memberPage.value = null;
  try {
    const result = await Applications.listRolesAsync(props.clientId, props.organizationId);
    if (requestId === rolesRequestId) {
      roles.value = result;
      sortRoles();
    }
  } catch {
    if (requestId === rolesRequestId) {
      loadError.value = t('app.applicationManagement.roles.loadFailed');
    }
  } finally {
    if (requestId === rolesRequestId) {
      isLoading.value = false;
    }
  }
}

async function selectRoleAsync(role: ApplicationRoleSummary): Promise<void> {
  if (selectedRoleId.value === role.id) {
    selectedRoleId.value = null;
    memberPage.value = null;
    ++membersRequestId;
    return;
  }

  selectedRoleId.value = role.id;
  memberIdentifier.value = '';
  memberActionError.value = null;
  await loadMembersAsync(1);
}

async function loadMembersAsync(page: number): Promise<void> {
  const roleIdValue = selectedRoleId.value;
  if (roleIdValue === null) {
    return;
  }

  const requestId = ++membersRequestId;
  areMembersLoading.value = true;
  membersError.value = null;
  try {
    const result = await Applications.listRoleMembersAsync(
      props.clientId,
      roleIdValue,
      page,
      pageSize,
      props.organizationId,
    );
    if (requestId === membersRequestId && selectedRoleId.value === roleIdValue) {
      memberPage.value = result;
    }
  } catch {
    if (requestId === membersRequestId) {
      membersError.value = t('app.applicationManagement.roles.membersLoadFailed');
    }
  } finally {
    if (requestId === membersRequestId) {
      areMembersLoading.value = false;
    }
  }
}

async function openCreateDialogAsync(): Promise<void> {
  roleId.value = '';
  roleName.value = '';
  roleIdError.value = null;
  roleNameError.value = null;
  createError.value = null;
  isCreateDialogOpen.value = true;
  await nextTick();
  requestAnimationFrame(() => roleIdInput.value?.focus());
}

function updateCreateDialogOpen(value: boolean): void {
  if (!isCreating.value) {
    isCreateDialogOpen.value = value;
  }
}

function validateRole(): boolean {
  const id = roleId.value.trim();
  const name = roleName.value.trim();
  roleIdError.value = null;
  roleNameError.value = null;

  if (id.length === 0) {
    roleIdError.value = t('app.applicationManagement.roles.errors.idRequired');
  } else if (id.length > 128) {
    roleIdError.value = t('app.applicationManagement.roles.errors.idTooLong');
  } else if (!/^[a-z0-9]+(?:[._:-][a-z0-9]+)*$/.test(id)) {
    roleIdError.value = t('app.applicationManagement.roles.errors.idInvalid');
  }

  if (name.length === 0) {
    roleNameError.value = t('app.applicationManagement.roles.errors.nameRequired');
  } else if (name.length > 128) {
    roleNameError.value = t('app.applicationManagement.roles.errors.nameTooLong');
  }

  return roleIdError.value === null && roleNameError.value === null;
}

async function createRoleAsync(): Promise<void> {
  if (isCreating.value || !validateRole()) {
    return;
  }

  isCreating.value = true;
  createError.value = null;
  try {
    const created = await Applications.createRoleAsync(
      props.clientId,
      roleId.value.trim(),
      roleName.value.trim(),
      props.organizationId,
    );
    roles.value.push(created);
    sortRoles();
    isCreateDialogOpen.value = false;
    await selectRoleAsync(created);
  } catch (error) {
    createError.value = error instanceof HttpStatusCodeError && error.status === 409
      ? t('app.applicationManagement.roles.errors.conflictOrLimit')
      : t('app.applicationManagement.roles.errors.createFailed');
  } finally {
    isCreating.value = false;
  }
}

async function addMemberAsync(): Promise<void> {
  const role = selectedRole.value;
  const accountId = memberIdentifier.value.trim();
  if (role === null || accountId.length === 0 || accountId.length > 128 || isAddingMember.value) {
    memberActionError.value = accountId.length === 0
      ? t('app.applicationManagement.roles.errors.accountRequired')
      : accountId.length > 128
        ? t('app.applicationManagement.roles.errors.accountTooLong')
        : null;
    return;
  }

  isAddingMember.value = true;
  memberActionError.value = null;
  try {
    await Applications.addRoleMemberAsync(
      props.clientId,
      role.id,
      accountId,
      props.organizationId,
    );
    role.memberCount += 1;
    memberIdentifier.value = '';
    await loadMembersAsync(memberPage.value?.page ?? 1);
  } catch (error) {
    memberActionError.value = error instanceof HttpStatusCodeError && error.status === 404
      ? t('app.applicationManagement.roles.errors.accountNotFound')
      : error instanceof HttpStatusCodeError && error.status === 409
        ? t('app.applicationManagement.roles.errors.alreadyAssigned')
        : t('app.applicationManagement.roles.errors.memberAddFailed');
  } finally {
    isAddingMember.value = false;
  }
}

async function removeMemberAsync(accountId: string): Promise<void> {
  const role = selectedRole.value;
  if (role === null || removingAccountId.value !== null) {
    return;
  }

  removingAccountId.value = accountId;
  memberActionError.value = null;
  try {
    await Applications.deleteRoleMemberAsync(
      props.clientId,
      role.id,
      accountId,
      props.organizationId,
    );
    role.memberCount = Math.max(0, role.memberCount - 1);
    const currentPage = memberPage.value?.page ?? 1;
    const nextPage = memberPage.value?.items.length === 1 && currentPage > 1
      ? currentPage - 1
      : currentPage;
    await loadMembersAsync(nextPage);
  } catch {
    memberActionError.value = t('app.applicationManagement.roles.errors.memberRemoveFailed');
  } finally {
    removingAccountId.value = null;
  }
}

function openDeleteDialog(role: ApplicationRoleSummary): void {
  deletingRole.value = role;
  deleteError.value = null;
  isDeleteDialogOpen.value = true;
}

function updateDeleteDialogOpen(value: boolean): void {
  if (!isDeleting.value) {
    isDeleteDialogOpen.value = value;
    if (!value) {
      deletingRole.value = null;
      deleteError.value = null;
    }
  }
}

async function deleteRoleAsync(): Promise<void> {
  const role = deletingRole.value;
  if (role === null || isDeleting.value) {
    return;
  }

  isDeleting.value = true;
  deleteError.value = null;
  try {
    await Applications.deleteRoleAsync(props.clientId, role.id, props.organizationId);
    roles.value = roles.value.filter(candidate => candidate.id !== role.id);
    if (selectedRoleId.value === role.id) {
      selectedRoleId.value = null;
      memberPage.value = null;
      ++membersRequestId;
    }
    isDeleteDialogOpen.value = false;
    deletingRole.value = null;
  } catch {
    deleteError.value = t('app.applicationManagement.roles.errors.deleteFailed');
  } finally {
    isDeleting.value = false;
  }
}

watch(
  () => [props.clientId, props.organizationId] as const,
  loadRolesAsync,
  { immediate: true },
);
</script>

<style scoped>
.roles-panel {
  width: 100%;
  margin-top: 14px;
  padding: 22px;
  box-sizing: border-box;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  box-shadow: var(--shadow-sm);
}

.panel-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
}

.panel-copy {
  min-width: 0;
}

.panel-title,
.members-title {
  margin: 0;
  color: var(--text-h);
  font-size: 17px;
  font-weight: 700;
  line-height: 1.4;
}

.panel-description {
  margin: 5px 0 0;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.create-button,
.add-member-button,
.dialog-action,
.page-button {
  width: auto;
  padding: 0 13px;
  grid-auto-flow: column;
  gap: 6px;
  font: inherit;
  font-size: 13px;
  font-weight: 700;
  white-space: nowrap;
}

.create-button {
  flex: 0 0 auto;
  color: var(--accent-hover);
  border-color: var(--accent-border);
  background: var(--accent-bg);
}

.scope-notice {
  display: flex;
  margin: 16px 0 0;
  padding: 10px 12px;
  align-items: flex-start;
  gap: 8px;
  border: 1px solid color-mix(in srgb, #f59e0b 38%, var(--border));
  border-radius: 8px;
  color: color-mix(in srgb, #f59e0b 72%, var(--text-h));
  background: color-mix(in srgb, #f59e0b 8%, var(--surface));
  font-size: 12px;
  line-height: 1.5;
}

.scope-notice .material-symbols-outlined {
  flex: 0 0 auto;
  font-size: 19px;
}

.panel-state,
.action-error {
  margin: 16px 0 0;
  font-size: 12px;
  line-height: 1.5;
}

.panel-state {
  color: var(--text-muted);
}

.panel-state.error,
.action-error {
  color: var(--danger);
}

.role-list,
.member-list {
  display: flex;
  margin: 18px 0 0;
  padding: 0;
  flex-direction: column;
  gap: 8px;
  list-style: none;
}

.role-row {
  display: grid;
  min-height: 58px;
  padding: 7px 8px 7px 13px;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: 10px;
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--surface);
  transition: border-color 160ms ease, background-color 160ms ease;
}

.role-row.selected {
  border-color: var(--accent-border);
  background: var(--accent-bg);
}

.role-select {
  display: flex;
  min-width: 0;
  padding: 5px 0;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  color: inherit;
  background: transparent;
  border: 0;
  text-align: left;
  cursor: pointer;
}

.role-identity {
  min-width: 0;
}

.role-name,
.role-id,
.member-name,
.member-identity {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.role-name,
.member-name {
  color: var(--text-h);
  font-size: 13px;
  font-weight: 750;
}

.role-id,
.member-identity {
  margin-top: 2px;
  color: var(--text-muted);
  font: 11px/1.4 var(--mono);
}

.role-count {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  gap: 4px;
  color: var(--text-muted);
  font-size: 11px;
  font-weight: 700;
}

.role-count .material-symbols-outlined {
  font-size: 17px;
}

.role-delete,
.member-remove {
  width: 38px;
  min-width: 38px;
  padding: 0;
  color: var(--text-muted);
}

.role-delete:hover:not(:disabled),
.member-remove:hover:not(:disabled) {
  color: var(--danger);
  border-color: color-mix(in srgb, var(--danger) 50%, var(--border));
  background: color-mix(in srgb, var(--danger) 8%, transparent);
}

.members-panel {
  margin-top: 16px;
  padding-top: 18px;
  border-top: 1px solid var(--border);
}

.members-description {
  margin: 4px 0 0;
  color: var(--text-muted);
  font-size: 12px;
}

.member-form {
  display: grid;
  margin-top: 14px;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px;
}

.member-input {
  min-width: 0;
  height: 40px;
  padding: 0 12px;
  box-sizing: border-box;
  border: 1.5px solid var(--border-strong);
  border-radius: 7px;
  outline: none;
  color: var(--text-h);
  background: var(--surface);
  font: 13px/1.5 var(--mono);
}

.member-input:focus {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--focus-ring);
}

.member-row {
  display: grid;
  min-height: 56px;
  padding: 8px 9px 8px 12px;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 12px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--surface);
}

.member-assigned-at {
  color: var(--text-muted);
  font-size: 11px;
  white-space: nowrap;
}

.pagination {
  display: flex;
  margin-top: 14px;
  align-items: center;
  justify-content: flex-end;
  gap: 9px;
}

.page-status {
  color: var(--text-muted);
  font-size: 11px;
}

.page-button {
  min-width: 38px;
  padding: 0 9px;
}

.dialog-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.dialog-description {
  margin: 0 0 16px;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.55;
}

.dialog-description code {
  display: inline;
  padding: 2px 5px;
}

.dialog-action.primary {
  color: var(--on-accent);
  border-color: var(--accent);
  background: var(--accent);
}

.dialog-action.danger {
  color: var(--danger);
  border-color: color-mix(in srgb, var(--danger) 48%, var(--border));
}

button:disabled,
input:disabled {
  cursor: not-allowed;
  opacity: 0.62;
}

@media (max-width: 640px) {
  .roles-panel {
    padding: 18px 16px;
  }

  .panel-header,
  .member-form {
    grid-template-columns: 1fr;
    align-items: stretch;
    flex-direction: column;
  }

  .create-button,
  .add-member-button {
    width: 100%;
  }

  .member-row {
    grid-template-columns: minmax(0, 1fr) auto;
  }

  .member-assigned-at {
    display: none;
  }
}
</style>

<template>
  <section class="roles-panel" aria-labelledby="application-roles-title">
    <div class="panel-header">
      <div class="panel-copy">
        <h2 id="application-roles-title" class="panel-title">
          {{ t('app.applicationManagement.roles.title') }}
        </h2>
        <p class="panel-description">
          {{ t('app.applicationManagement.roles.description') }}
        </p>
      </div>
      <button
        type="button"
        class="app-button create-button"
        :disabled="isLoading || roles.length >= 100"
        @click="openCreateDialogAsync"
      >
        <span class="material-symbols-outlined" aria-hidden="true">add_moderator</span>
        <span>{{ t('app.applicationManagement.roles.createAction') }}</span>
      </button>
    </div>

    <p v-if="!rolesScopeEnabled" class="scope-notice">
      <span class="material-symbols-outlined" aria-hidden="true">info</span>
      <span>{{ t('app.applicationManagement.roles.scopeDisabled') }}</span>
    </p>

    <p v-if="isLoading" class="panel-state" role="status">
      {{ t('app.applicationManagement.roles.loading') }}
    </p>
    <p v-else-if="loadError" class="panel-state error" role="alert">{{ loadError }}</p>
    <p v-else-if="roles.length === 0" class="panel-state">
      {{ t('app.applicationManagement.roles.empty') }}
    </p>
    <ul v-else class="role-list">
      <li
        v-for="role in roles"
        :key="role.id"
        class="role-row"
        :class="{ selected: selectedRoleId === role.id }"
      >
        <button type="button" class="role-select" @click="selectRoleAsync(role)">
          <span class="role-identity">
            <span class="role-name">{{ role.name }}</span>
            <code class="role-id">{{ role.id }}</code>
          </span>
          <span class="role-count">
            <span class="material-symbols-outlined" aria-hidden="true">person</span>
            {{ role.memberCount }}
          </span>
        </button>
        <button
          type="button"
          class="app-button role-delete"
          :disabled="isDeleting"
          :aria-label="t('app.applicationManagement.roles.deleteLabel', { name: role.name })"
          @click="openDeleteDialog(role)"
        >
          <span class="material-symbols-outlined" aria-hidden="true">delete</span>
        </button>
      </li>
    </ul>

    <section v-if="selectedRole" class="members-panel" aria-labelledby="role-members-title">
      <h3 id="role-members-title" class="members-title">
        {{ t('app.applicationManagement.roles.membersTitle', { name: selectedRole.name }) }}
      </h3>
      <p class="members-description">
        {{ t('app.applicationManagement.roles.membersDescription') }}
      </p>
      <form class="member-form" @submit.prevent="addMemberAsync">
        <input
          v-model="memberIdentifier"
          class="member-input"
          :placeholder="t('app.applicationManagement.roles.accountPlaceholder')"
          :disabled="isAddingMember"
          autocomplete="off"
        />
        <button type="submit" class="app-button add-member-button" :disabled="isAddingMember">
          <span class="material-symbols-outlined" aria-hidden="true">person_add</span>
          <span>{{ t('app.applicationManagement.roles.addMember') }}</span>
        </button>
      </form>
      <p v-if="memberActionError" class="action-error" role="alert">{{ memberActionError }}</p>

      <p v-if="areMembersLoading" class="panel-state" role="status">
        {{ t('app.applicationManagement.roles.membersLoading') }}
      </p>
      <p v-else-if="membersError" class="panel-state error" role="alert">
        {{ membersError }}
      </p>
      <p v-else-if="memberPage?.items.length === 0" class="panel-state">
        {{ t('app.applicationManagement.roles.membersEmpty') }}
      </p>
      <ul v-else-if="memberPage" class="member-list">
        <li v-for="member in memberPage.items" :key="member.accountId" class="member-row">
          <span>
            <span class="member-name">{{ member.name }}</span>
            <span class="member-identity">{{ member.accountId }} · {{ member.email }}</span>
          </span>
          <span class="member-assigned-at">
            {{ t('app.applicationManagement.roles.assignedAt', {
              date: formatAssignedAt(member.assignedAt),
            }) }}
          </span>
          <button
            type="button"
            class="app-button member-remove"
            :disabled="removingAccountId !== null"
            :aria-label="t('app.applicationManagement.roles.removeMemberLabel', {
              name: member.name,
            })"
            @click="removeMemberAsync(member.accountId)"
          >
            <span class="material-symbols-outlined" aria-hidden="true">person_remove</span>
          </button>
        </li>
      </ul>

      <nav v-if="memberPage && totalPages > 1" class="pagination" aria-label="Role members">
        <button
          type="button"
          class="app-button page-button"
          :disabled="areMembersLoading || memberPage.page <= 1"
          @click="loadMembersAsync(memberPage.page - 1)"
        >
          <span class="material-symbols-outlined" aria-hidden="true">chevron_left</span>
        </button>
        <span class="page-status">{{ memberPage.page }} / {{ totalPages }}</span>
        <button
          type="button"
          class="app-button page-button"
          :disabled="areMembersLoading || memberPage.page >= totalPages"
          @click="loadMembersAsync(memberPage.page + 1)"
        >
          <span class="material-symbols-outlined" aria-hidden="true">chevron_right</span>
        </button>
      </nav>
    </section>

    <Dialog
      :is-open="isCreateDialogOpen"
      :title="t('app.applicationManagement.roles.createTitle')"
      size="small"
      :close-on-backdrop="!isCreating"
      :close-on-escape="!isCreating"
      :show-close-button="!isCreating"
      @update:is-open="updateCreateDialogOpen"
    >
      <form id="create-application-role-form" class="dialog-form" @submit.prevent="createRoleAsync">
        <FloatingInput
          ref="roleIdInput"
          v-model="roleId"
          :label="t('app.applicationManagement.roles.id')"
          :hint="t('app.applicationManagement.roles.idHint')"
          :error="roleIdError ?? false"
          :disabled="isCreating"
          autocomplete="off"
        />
        <FloatingInput
          v-model="roleName"
          :label="t('app.applicationManagement.roles.name')"
          :error="roleNameError ?? false"
          :disabled="isCreating"
          autocomplete="off"
        />
        <p v-if="createError" class="action-error" role="alert">{{ createError }}</p>
      </form>
      <template #footer>
        <button
          type="button"
          class="app-button dialog-action"
          :disabled="isCreating"
          @click="updateCreateDialogOpen(false)"
        >
          {{ t('app.applicationManagement.roles.cancel') }}
        </button>
        <button
          type="submit"
          form="create-application-role-form"
          class="app-button dialog-action primary"
          :disabled="isCreating"
        >
          {{ t('app.applicationManagement.roles.createSubmit') }}
        </button>
      </template>
    </Dialog>

    <Dialog
      :is-open="isDeleteDialogOpen"
      :title="t('app.applicationManagement.roles.deleteTitle')"
      size="small"
      :close-on-backdrop="!isDeleting"
      :close-on-escape="!isDeleting"
      :show-close-button="!isDeleting"
      @update:is-open="updateDeleteDialogOpen"
    >
      <p v-if="deletingRole" class="dialog-description">
        <i18n-t keypath="app.applicationManagement.roles.deleteDescription" tag="span">
          <template #role><code>{{ deletingRole.id }}</code></template>
        </i18n-t>
      </p>
      <p v-if="deleteError" class="action-error" role="alert">{{ deleteError }}</p>
      <template #footer>
        <button
          type="button"
          class="app-button dialog-action"
          :disabled="isDeleting"
          @click="updateDeleteDialogOpen(false)"
        >
          {{ t('app.applicationManagement.roles.cancel') }}
        </button>
        <button
          type="button"
          class="app-button dialog-action danger"
          :disabled="isDeleting"
          @click="deleteRoleAsync"
        >
          {{ t('app.applicationManagement.roles.deleteConfirm') }}
        </button>
      </template>
    </Dialog>
  </section>
</template>
