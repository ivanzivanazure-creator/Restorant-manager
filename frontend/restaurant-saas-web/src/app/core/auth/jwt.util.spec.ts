import { decodeAccessToken, isExpired, toStringArray } from './jwt.util';

function base64UrlEncode(json: object): string {
  const base64 = btoa(JSON.stringify(json));
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fakeJwt(payload: object): string {
  const header = base64UrlEncode({ alg: 'HS256', typ: 'JWT' });
  const body = base64UrlEncode(payload);
  return `${header}.${body}.fake-signature`;
}

describe('jwt.util', () => {
  it('decodeAccessToken decodes a valid token payload', () => {
    const token = fakeJwt({ sub: 'user-1', email: 'test@demo.io', exp: 9999999999, role: 'Owner' });

    const decoded = decodeAccessToken(token);

    expect(decoded?.sub).toBe('user-1');
    expect(decoded?.email).toBe('test@demo.io');
  });

  it('decodeAccessToken returns null for a malformed token', () => {
    expect(decodeAccessToken('not-a-jwt')).toBeNull();
  });

  it('isExpired returns true once past the exp claim', () => {
    const decoded = { sub: 'x', email: 'x', role: 'x', permission: 'x', exp: Math.floor(Date.now() / 1000) - 60 };
    expect(isExpired(decoded)).toBeTrue();
  });

  it('isExpired returns false while still valid', () => {
    const decoded = { sub: 'x', email: 'x', role: 'x', permission: 'x', exp: Math.floor(Date.now() / 1000) + 3600 };
    expect(isExpired(decoded)).toBeFalse();
  });

  it('toStringArray normalizes a single string or an array', () => {
    expect(toStringArray('a')).toEqual(['a']);
    expect(toStringArray(['a', 'b'])).toEqual(['a', 'b']);
    expect(toStringArray(undefined)).toEqual([]);
  });
});
