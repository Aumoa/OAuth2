export const supportedLocales = ['ko', 'en', 'es'] as const;

export type SupportedLocale = typeof supportedLocales[number];

export const defaultLocale: SupportedLocale = 'en';
export const localeStorageKey = 'preference-language';

export function toSupportedLocale(value: string | null | undefined): SupportedLocale | null {
  const languageCode = value?.trim().toLowerCase().split('-')[0];

  return supportedLocales.find((locale) => locale === languageCode) ?? null;
}

export function resolveSystemLocale(): SupportedLocale | null {
  return toSupportedLocale(globalThis.navigator?.language);
}

export function resolvePreferredLocale(): SupportedLocale {
  try {
    const storedLocale = toSupportedLocale(globalThis.localStorage.getItem(localeStorageKey));

    if (storedLocale) {
      return storedLocale;
    }
  } catch {
    // localStorage may be unavailable in privacy-restricted browser contexts.
  }

  const browserLanguages = globalThis.navigator?.languages?.length
    ? globalThis.navigator.languages
    : [globalThis.navigator?.language];

  for (const language of browserLanguages) {
    const browserLocale = toSupportedLocale(language);

    if (browserLocale) {
      return browserLocale;
    }
  }

  return defaultLocale;
}

export function persistLocale(locale: SupportedLocale): void {
  globalThis.document.documentElement.lang = locale;

  try {
    globalThis.localStorage.setItem(localeStorageKey, locale);
  } catch {
    // The active locale still works for this session when storage is unavailable.
  }
}
