<script setup lang="ts">
import { computed, ref, useId, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  defaultLocale,
  persistLocale,
  resolveSystemLocale,
  toSupportedLocale,
  type SupportedLocale,
} from '../i18n/locale';
import Dialog from './Dialog.vue';

interface LanguageOption {
  code: SupportedLocale;
  badge: string;
  nativeName: string;
}

interface LanguageSection {
  id: 'recommended' | 'all' | 'results';
  label: string;
  languages: readonly LanguageOption[];
  recommended: boolean;
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
  { code: 'ko', badge: 'KO', nativeName: '한국어' },
  { code: 'en', badge: 'EN', nativeName: 'English' },
  { code: 'es', badge: 'ES', nativeName: 'Español' },
];

const languageResultsId = `language-results-${useId()}`;
const isDialogOpen = ref(false);
const searchQuery = ref('');
const selectedLanguage = computed(() => toSupportedLocale(locale.value) ?? defaultLocale);
const selectedLanguageOption = computed(() => (
  languages.find((language) => language.code === selectedLanguage.value) ?? languages[0]
));
const systemDefaultLanguage = resolveSystemLocale();
const systemLanguageOption = languages.find(
  (language) => language.code === systemDefaultLanguage,
);
const isSearching = computed(() => searchQuery.value.trim().length > 0);

function getTranslatedName(language: LanguageOption): string {
  return t(`core.languageSelector.languageNames.${language.code}`);
}

function normalizeSearchValue(value: string): string {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLocaleLowerCase();
}

const filteredLanguages = computed(() => {
  const query = normalizeSearchValue(searchQuery.value.trim());

  if (!query) {
    return languages;
  }

  return languages.filter((language) => [
    language.code,
    language.nativeName,
    getTranslatedName(language),
  ].some((value) => normalizeSearchValue(value).includes(query)));
});

const languageSections = computed<LanguageSection[]>(() => {
  if (isSearching.value) {
    return [{
      id: 'results',
      label: t('core.languageSelector.searchResults'),
      languages: filteredLanguages.value,
      recommended: false,
    }];
  }

  const sections: LanguageSection[] = [];

  if (systemLanguageOption) {
    sections.push({
      id: 'recommended',
      label: t('core.languageSelector.recommended'),
      languages: [systemLanguageOption],
      recommended: true,
    });
  }

  sections.push({
    id: 'all',
    label: t('core.languageSelector.allLanguages'),
    languages: systemLanguageOption
      ? languages.filter((language) => language.code !== systemLanguageOption.code)
      : languages,
    recommended: false,
  });

  return sections;
});

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

watch(isDialogOpen, (isOpen) => {
  if (!isOpen) {
    searchQuery.value = '';
  }
});
</script>

<template>
  <button
    type="button"
    class="app-button language-trigger"
    :aria-label="`${t('core.languageSelector.changeLabel')}: ${selectedLanguageOption.nativeName}`"
    :title="t('core.languageSelector.changeLabel')"
    aria-haspopup="dialog"
    :aria-expanded="isDialogOpen"
    @click="openDialog"
  >
    <span class="material-symbols-outlined" aria-hidden="true">language</span>
    <span class="language-trigger-code" aria-hidden="true">
      {{ selectedLanguageOption.badge }}
    </span>
  </button>

  <Dialog
    v-model:isOpen="isDialogOpen"
    :title="t('core.languageSelector.dialogTitle')"
    :close-label="t('core.languageSelector.closeLabel')"
    size="medium"
  >
    <div class="language-picker">
      <p class="language-description">{{ t('core.languageSelector.description') }}</p>

      <label class="language-search">
        <span class="material-symbols-outlined" aria-hidden="true">search</span>
        <input
          v-model="searchQuery"
          type="search"
          :placeholder="t('core.languageSelector.searchPlaceholder')"
          :aria-label="t('core.languageSelector.searchPlaceholder')"
          :aria-controls="languageResultsId"
          autocomplete="off"
          autofocus
        />
        <button
          v-if="searchQuery"
          type="button"
          class="language-search-clear"
          :aria-label="t('core.languageSelector.clearSearch')"
          :title="t('core.languageSelector.clearSearch')"
          @click="searchQuery = ''"
        >
          <span class="material-symbols-outlined" aria-hidden="true">close</span>
        </button>
      </label>

      <div
        :id="languageResultsId"
        class="language-results"
        :aria-label="t('core.languageSelector.availableLanguages')"
      >
        <section
          v-for="section in languageSections"
          :key="section.id"
          class="language-section"
        >
          <h3 class="language-section-title">{{ section.label }}</h3>

          <ul class="language-list">
            <li v-for="language in section.languages" :key="language.code">
              <button
                type="button"
                class="language-option"
                :class="{
                  'language-option--selected': selectedLanguage === language.code,
                  'language-option--recommended': section.recommended,
                }"
                :aria-pressed="selectedLanguage === language.code"
                @click="selectLanguage(language.code)"
              >
                <span class="language-option-leading" aria-hidden="true">
                  <span v-if="section.recommended" class="material-symbols-outlined">computer</span>
                  <span v-else>{{ language.badge }}</span>
                </span>

                <span class="language-label">
                  <strong :lang="language.code">{{ language.nativeName }}</strong>
                  <small>
                    <span v-if="getTranslatedName(language) !== language.nativeName">
                      {{ getTranslatedName(language) }} ·
                    </span>
                    <span>{{ language.badge }}</span>
                    <span v-if="section.recommended">
                      · {{ t('core.languageSelector.systemDefault') }}
                    </span>
                  </small>
                </span>

                <span
                  class="material-symbols-outlined language-check"
                  aria-hidden="true"
                >check</span>
              </button>
            </li>
          </ul>
        </section>

        <p
          v-if="isSearching && filteredLanguages.length === 0"
          class="language-empty"
          role="status"
        >
          <span class="material-symbols-outlined" aria-hidden="true">search_off</span>
          {{ t('core.languageSelector.noResults') }}
        </p>
      </div>
    </div>
  </Dialog>
</template>

<style scoped>
.language-trigger {
  width: auto;
  min-width: 40px;
  grid-auto-flow: column;
  gap: 5px;
  padding-inline: 9px;
}

.language-trigger-code {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.language-picker {
  display: grid;
  gap: 16px;
}

.language-description {
  margin: 0;
  color: var(--text);
  font-size: 0.925rem;
}

.language-search {
  display: grid;
  min-height: 46px;
  grid-template-columns: 24px minmax(0, 1fr) 32px;
  align-items: center;
  gap: 8px;
  padding: 0 8px 0 13px;
  color: var(--text);
  background: var(--code-bg);
  border: 1px solid var(--border);
  border-radius: 10px;
  transition: border-color 150ms ease, box-shadow 150ms ease;
}

.language-search:focus-within {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--accent-bg);
}

.language-search > .material-symbols-outlined {
  font-size: 21px;
}

.language-search input {
  width: 100%;
  min-width: 0;
  padding: 11px 0;
  color: var(--text-h);
  background: transparent;
  border: 0;
  outline: 0;
  font: inherit;
}

.language-search input::placeholder {
  color: var(--text);
  opacity: 0.72;
}

.language-search input::-webkit-search-cancel-button {
  display: none;
}

.language-search-clear {
  display: inline-grid;
  width: 32px;
  height: 32px;
  padding: 0;
  place-items: center;
  color: var(--text);
  background: transparent;
  border: 0;
  border-radius: 8px;
  cursor: pointer;
}

.language-search-clear:hover {
  color: var(--text-h);
  background: var(--accent-bg);
}

.language-search-clear:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 1px;
}

.language-search-clear .material-symbols-outlined {
  font-size: 19px;
}

.language-results {
  max-height: min(52svh, 420px);
  margin-inline: -8px;
  padding-inline: 8px;
  overflow-y: auto;
  overscroll-behavior: contain;
  scrollbar-gutter: stable;
}

.language-section + .language-section {
  margin-top: 18px;
}

.language-section-title {
  margin: 0 0 6px;
  padding-inline: 10px;
  color: var(--text);
  font-size: 0.72rem;
  font-weight: 650;
  letter-spacing: 0.055em;
  text-transform: uppercase;
}

.language-list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.language-list li + li {
  border-top: 1px solid var(--border);
}

.language-option {
  display: grid;
  width: 100%;
  min-height: 58px;
  grid-template-columns: 36px minmax(0, 1fr) 28px;
  align-items: center;
  gap: 12px;
  padding: 8px 10px;
  text-align: left;
  color: var(--text);
  background: transparent;
  border: 0;
  border-radius: 9px;
  cursor: pointer;
  transition: color 140ms ease, background-color 140ms ease;
}

.language-option:hover {
  color: var(--text-h);
  background: var(--code-bg);
}

.language-option:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: -2px;
}

.language-option--recommended {
  color: var(--text-h);
  background: var(--accent-bg);
}

.language-option--selected {
  color: var(--text-h);
  background: var(--accent-bg);
}

.language-option-leading {
  display: inline-grid;
  width: 32px;
  height: 32px;
  place-items: center;
  color: var(--text);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.language-option--recommended .language-option-leading {
  color: var(--accent);
}

.language-option-leading .material-symbols-outlined {
  font-size: 21px;
}

.language-label {
  display: grid;
  min-width: 0;
  gap: 2px;
}

.language-label strong {
  overflow: hidden;
  color: inherit;
  font-size: 0.95rem;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.language-label small {
  overflow: hidden;
  color: var(--text);
  font-size: 0.75rem;
  text-overflow: ellipsis;
  white-space: nowrap;
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

.language-empty {
  display: grid;
  min-height: 132px;
  margin: 0;
  place-items: center;
  align-content: center;
  gap: 8px;
  color: var(--text);
  text-align: center;
  font-size: 0.875rem;
}

.language-empty .material-symbols-outlined {
  font-size: 30px;
  opacity: 0.7;
}

@media (max-width: 480px) {
  .language-results {
    max-height: 48svh;
  }
}

@media (prefers-reduced-motion: reduce) {
  .language-search,
  .language-option,
  .language-check {
    transition-duration: 0.01ms;
  }
}
</style>
