import { createI18n } from 'vue-i18n';
import { resolvePreferredLocale } from '../core/i18n/locale';
import { coreMessages } from '../core/i18n/messages';
import { appMessages } from './messages';

const locale = resolvePreferredLocale();

globalThis.document.documentElement.lang = locale;

export const i18n = createI18n({
  legacy: false,
  locale,
  fallbackLocale: 'en',
  messages: {
    ko: {
      ...appMessages.ko,
      ...coreMessages.ko,
    },
    en: {
      ...appMessages.en,
      ...coreMessages.en,
    },
    es: {
      ...appMessages.es,
      ...coreMessages.es,
    },
  },
});
