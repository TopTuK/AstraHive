import { ref } from 'vue';

const STORAGE_KEY = 'astrahive-theme';
const THEMES = ['light', 'dark'];

function readInitialTheme() {
    try {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (THEMES.includes(saved)) return saved;
    } catch {
        // Storage can be unavailable (private mode); fall back to the OS preference.
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export const currentTheme = ref(readInitialTheme());

export function setTheme(theme) {
    if (!THEMES.includes(theme)) return;

    currentTheme.value = theme;
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
    try {
        localStorage.setItem(STORAGE_KEY, theme);
    } catch {
        // Persisting is best-effort.
    }
}

export function toggleTheme() {
    setTheme(currentTheme.value === 'dark' ? 'light' : 'dark');
}
