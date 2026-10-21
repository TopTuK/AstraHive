import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useWorkspaceStore } from './workspaceStore';

describe('workspaceStore', () => {
    beforeEach(() => {
        window.localStorage.clear();
        setActivePinia(createPinia());
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('starts without a selection', () => {
        expect(useWorkspaceStore().selectedId).toBeNull();
    });

    it('remembers the selected workspace across stores', () => {
        useWorkspaceStore().select(7);

        setActivePinia(createPinia());

        expect(useWorkspaceStore().selectedId).toBe(7);
    });

    it('forgets the selection on clear', () => {
        const store = useWorkspaceStore();
        store.select(7);

        store.clear();

        expect(store.selectedId).toBeNull();
        expect(window.localStorage.getItem('astrahive.workspace')).toBeNull();
    });

    it('still selects when storage is unavailable', () => {
        vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
            throw new Error('blocked');
        });
        const store = useWorkspaceStore();

        store.select(3);

        expect(store.selectedId).toBe(3);
    });
});
