<script setup lang="ts">
import { onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { Sessions } from '../../api/Sessions.ts';
import { router } from '../../router/index.ts';

const { t } = useI18n({ useScope: 'global' });

async function logoutAsync(): Promise<void> {
  try {
    await Sessions.deleteAsync();
    window.location.replace('/api/v1/auth/login');
  } catch {
    await router.replace({
      path: '/error',
      query: {
        message: t('oauth2.profileCard.logoutFailed'),
      },
    });
  }
}

onMounted(() => {
  void logoutAsync();
});
</script>

<template>
  <p role="status">{{ t('oauth2.profileCard.loggingOut') }}</p>
</template>
