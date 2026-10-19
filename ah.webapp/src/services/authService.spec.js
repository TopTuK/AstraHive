import { describe, expect, it } from 'vitest';
import { loginUrl } from './authService';

describe('loginUrl', () => {
    it('builds the provider challenge URL with an encoded returnUrl', () => {
        expect(loginUrl('google', '/welcome')).toBe('/api/auth/login/google?returnUrl=%2Fwelcome');
    });

    it('encodes the provider and query characters', () => {
        expect(loginUrl('ya ndex', '/clusters?id=1&x=2')).toBe(
            '/api/auth/login/ya%20ndex?returnUrl=%2Fclusters%3Fid%3D1%26x%3D2',
        );
    });

    it('omits the query string without a returnUrl', () => {
        expect(loginUrl('yandex')).toBe('/api/auth/login/yandex');
    });
});
