import { DecodedAccessToken } from '../models/auth.models';

/** Decodes (without verifying — verification happens server-side) a JWT's payload for UI purposes:
 * showing the user's name, gating nav items by role/permission, and reading the expiry to schedule refresh. */
export function decodeAccessToken(token: string): DecodedAccessToken | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;

  try {
    const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = payload.padEnd(payload.length + ((4 - (payload.length % 4)) % 4), '=');
    const json = decodeURIComponent(
      atob(padded)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    return JSON.parse(json) as DecodedAccessToken;
  } catch {
    return null;
  }
}

export function isExpired(decoded: DecodedAccessToken, skewSeconds = 10): boolean {
  return Date.now() / 1000 > decoded.exp - skewSeconds;
}

export function toStringArray(value: string | string[] | undefined): string[] {
  if (!value) return [];
  return Array.isArray(value) ? value : [value];
}
