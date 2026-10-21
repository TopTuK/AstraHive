import { defineStore } from 'pinia';
import { ref } from 'vue';

const STORAGE_KEY = 'astrahive.workspace';

// Storage can be unavailable (private mode, blocked site data); the selection is only a convenience.
function readStoredId() {
    try {
        const value = Number(window.localStorage.getItem(STORAGE_KEY));
        return Number.isInteger(value) && value > 0 ? value : null;
    } catch {
        return null;
    }
}

function writeStoredId(id) {
    try {
        if (id === null) window.localStorage.removeItem(STORAGE_KEY);
        else window.localStorage.setItem(STORAGE_KEY, String(id));
    } catch {
        // Ignored: the selection then lasts until the page reloads.
    }
}

// Which workspace the user is working in. The workspaces themselves are server data (useWorkspaces).
export const useWorkspaceStore = defineStore('workspace', () => {
    const selectedId = ref(readStoredId());

    const select = (id) => {
        selectedId.value = id;
        writeStoredId(id);
    };

    const clear = () => select(null);

    return { selectedId, select, clear };
});
