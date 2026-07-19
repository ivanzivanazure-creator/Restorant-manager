import { Injectable } from '@angular/core';
import { AuthTokens } from '../models/auth.models';

const ACCESS_TOKEN_KEY = 'rsaas.accessToken';
const REFRESH_TOKEN_KEY = 'rsaas.refreshToken';

/** localStorage-backed token persistence. Kept behind a service so AuthService/interceptors never touch
 * `localStorage` directly, making it easy to swap for a more secure storage strategy later (e.g. an
 * httpOnly cookie + BFF pattern) without touching call sites. */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  get accessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  store(tokens: AuthTokens): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }
}
