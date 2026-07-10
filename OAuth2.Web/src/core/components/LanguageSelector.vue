<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  defaultLocale,
  persistLocale,
  toSupportedLocale,
  type SupportedLocale,
} from '../i18n/locale';
import Dialog from './Dialog.vue';

interface LanguageOption {
  code: SupportedLocale;
  badge: string;
  nativeName: string;
  englishName: string;
}

const props = defineProps<{
  modelValue?: SupportedLocale;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: SupportedLocale];
  change: [value: SupportedLocale];
}>();

const { locale, t } = useI18n({ useScope: 'global' });

const languages: readonly LanguageOption[] = [
  { code: 'ko', badge: 'KO', nativeName: '한국어', englishName: 'Korean' },
  { code: 'en', badge: 'EN', nativeName: 'English', englishName: 'English' },
  { code: 'es', badge: 'ES', nativeName: 'Español', englishName: 'Spanish' },
];

const isDialogOpen = ref(false);
const selectedLanguage = computed(() => toSupportedLocale(locale.value) ?? defaultLocale);

function openDialog() {
  isDialogOpen.value = true;
}

function applyLanguage(language: SupportedLocale) {
  locale.value = language;
  persistLocale(language);
}

function selectLanguage(language: SupportedLocale) {
  applyLanguage(language);
  emit('update:modelValue', language);
  emit('change', language);
  isDialogOpen.value = false;
}

watch(() => props.modelValue, (language) => {
  if (language) {
    applyLanguage(language);
  }
}, { immediate: true });
</script>

<template>
  <button
    type="button"
    class="icon-button"
    :aria-label="t('core.languageSelector.changeLabel')"
    :title="t('core.languageSelector.changeLabel')"
    aria-haspopup="dialog"
    :aria-expanded="isDialogOpen"
    @click="openDialog"
  >
    <span class="material-symbols-outlined" aria-hidden="true">language_korean_latin</span>
  </button>

  <Dialog
    v-model:isOpen="isDialogOpen"
    :title="t('core.languageSelector.dialogTitle')"
    :close-label="t('core.languageSelector.closeLabel')"
    size="small"
  >
    <div class="language-picker">
      <p class="language-description">{{ t('core.languageSelector.description') }}</p>

      <div class="language-list" :aria-label="t('core.languageSelector.availableLanguages')">
        <button
          v-for="language in languages"
          :key="language.code"
          type="button"
          class="language-option"
          :class="{ 'language-option--selected': selectedLanguage === language.code }"
          :aria-pressed="selectedLanguage === language.code"
          @click="selectLanguage(language.code)"
        >
          <span class="language-badge" aria-hidden="true">{{ language.badge }}</span>

          <span class="language-label">
            <strong>{{ language.nativeName }}</strong>
            <small v-if="language.nativeName !== language.englishName">
              {{ language.englishName }}
            </small>
          </span>

          <span
            class="material-symbols-outlined language-check"
            aria-hidden="true"
          >check</span>
        </button>
      </div>
    </div>
  </Dialog>
</template>

<style scoped>

.language-picker {
  display: grid;
  gap: 20px;
}

.language-description {
  margin: 0;
  color: var(--text);
  font-size: 0.925rem;
}

.language-list {
  display: grid;
  gap: 10px;
}

.language-option {
  display: grid;
  width: 100%;
  min-height: 68px;
  grid-template-columns: 44px minmax(0, 1fr) 24px;
  align-items: center;
  gap: 14px;
  padding: 10px 14px 10px 10px;
  text-align: left;
  color: var(--text);
  background: var(--oauth-surface-muted, var(--code-bg));
  border: 1px solid var(--border);
  border-radius: 13px;
  cursor: pointer;
  transition:
    color 150ms ease,
    background-color 150ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease,
    transform 150ms ease;
}

.language-option:hover {
  color: var(--text-h);
  background: var(--accent-bg);
  border-color: var(--accent-border);
  transform: translateY(-1px);
}

.language-option:focus-visible {
  outline: 3px solid var(--accent-border);
  outline-offset: 2px;
}

.language-option--selected {
  color: var(--text-h);
  background: var(--accent-bg);
  border-color: var(--accent-border);
  box-shadow: inset 3px 0 0 var(--accent);
}

.language-badge {
  display: inline-grid;
  width: 42px;
  height: 42px;
  place-items: center;
  color: var(--text-h);
  background: var(--oauth-surface, var(--bg));
  border: 1px solid var(--border);
  border-radius: 11px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.language-option--selected .language-badge {
  color: var(--accent);
  border-color: var(--accent-border);
}

.language-label {
  display: grid;
  min-width: 0;
  gap: 2px;
}

.language-label strong {
  overflow: hidden;
  color: inherit;
  font-size: 0.975rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.language-label small {
  color: var(--text);
  font-size: 0.78rem;
}

.language-check {
  color: var(--accent);
  font-size: 21px;
  opacity: 0;
  transform: scale(0.75);
  transition: opacity 140ms ease, transform 140ms ease;
}

.language-option--selected .language-check {
  opacity: 1;
  transform: scale(1);
}

@media (prefers-reduced-motion: reduce) {
  .language-trigger,
  .language-option,
  .language-check {
    transition-duration: 0.01ms;
  }

  .language-option:hover {
    transform: none;
  }
}
</style>
