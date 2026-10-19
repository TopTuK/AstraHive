import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/vue';
import { afterEach, vi } from 'vitest';

// jsdom has no matchMedia; src/theme reads it at import time.
if (!window.matchMedia) {
    window.matchMedia = vi.fn().mockImplementation((query) => ({
        matches: false,
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
    }));
}

// jsdom does not implement scrolling; the router's scrollBehavior calls it on every navigation.
window.scrollTo = vi.fn();

afterEach(() => {
    cleanup();
});
