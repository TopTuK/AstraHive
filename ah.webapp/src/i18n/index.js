import { createI18n } from 'vue-i18n';
import en from './locales/en.json';
import ru from './locales/ru.json';

const STORAGE_KEY = 'astrahive-locale';

export const SUPPORTED_LOCALES = ['en', 'ru'];

function readInitialLocale() {
    try {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (SUPPORTED_LOCALES.includes(saved)) return saved;
    } catch {
        // Storage can be unavailable (private mode); fall back to the browser language.
    }
    const browser = navigator.language?.slice(0, 2);
    return SUPPORTED_LOCALES.includes(browser) ? browser : 'en';
}

export const i18n = createI18n({
    legacy: false,
    locale: readInitialLocale(),
    fallbackLocale: 'en',
    messages: { en, ru },
});

export function setLocale(locale) {
    if (!SUPPORTED_LOCALES.includes(locale)) return;

    i18n.global.locale.value = locale;
    document.documentElement.lang = locale;
    try {
        localStorage.setItem(STORAGE_KEY, locale);
    } catch {
        // Persisting is best-effort.
    }
}
