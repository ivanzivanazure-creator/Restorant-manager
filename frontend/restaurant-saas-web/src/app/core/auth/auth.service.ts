import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthTokens,
  CurrentUser,
  LoginRequest,
  LoginResult,
  MfaEnrollmentResult,
  RegisterOwnerRequest,
  VerifyMfaRequest,
} from '../models/auth.models';
import { decodeAccessToken, toStringArray } from './jwt.util';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSignal = signal<CurrentUser | null>(this.readUserFromStorage());

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

  constructor(
    private readonly http: HttpClient,
    private readonly tokens: TokenStorageService,
    private readonly router: Router,
  ) {}

  register(request: RegisterOwnerRequest): Observable<AuthTokens> {
    return this.http
      .post<AuthTokens>(`${environment.apiBaseUrl}/auth/register`, request)
      .pipe(tap((result) => this.applyTokens(result)));
  }

  login(email: string, password: string): Observable<LoginResult> {
    const request: LoginRequest = { email, password, deviceInfo: this.deviceInfo() };
    return this.http.post<LoginResult>(`${environment.apiBaseUrl}/auth/login`, request).pipe(
      tap((result) => {
        if (result.tokens) this.applyTokens(result.tokens);
      }),
    );
  }

  verifyMfa(mfaChallengeToken: string, code: string): Observable<AuthTokens> {
    const request: VerifyMfaRequest = { mfaChallengeToken, code, deviceInfo: this.deviceInfo() };
    return this.http
      .post<AuthTokens>(`${environment.apiBaseUrl}/auth/mfa/verify`, request)
      .pipe(tap((result) => this.applyTokens(result)));
  }

  enrollMfa(): Observable<MfaEnrollmentResult> {
    return this.http.post<MfaEnrollmentResult>(`${environment.apiBaseUrl}/auth/mfa/enroll`, {});
  }

  confirmMfaEnrollment(code: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/mfa/confirm`, { code });
  }

  forgotPassword(email: string, resetUrlTemplate: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/forgot-password`, { email, resetUrlTemplate });
  }

  resetPassword(userId: string, token: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/reset-password`, { userId, token, newPassword });
  }

  refresh(): Observable<AuthTokens> {
    const refreshToken = this.tokens.refreshToken;
    return this.http
      .post<AuthTokens>(`${environment.apiBaseUrl}/auth/refresh`, { refreshToken, deviceInfo: this.deviceInfo() })
      .pipe(tap((result) => this.applyTokens(result)));
  }

  logout(): void {
    const refreshToken = this.tokens.refreshToken;
    this.tokens.clear();
    this.currentUserSignal.set(null);
    if (refreshToken) {
      this.http.post(`${environment.apiBaseUrl}/auth/logout`, { refreshToken }).subscribe({ error: () => void 0 });
    }
    this.router.navigate(['/auth/login']);
  }

  hasPermission(permission: string): boolean {
    const user = this.currentUserSignal();
    return !!user && (user.isSuperAdmin || user.permissions.includes(permission));
  }

  private applyTokens(tokens: AuthTokens): void {
    this.tokens.store(tokens);
    this.currentUserSignal.set(this.decodeUser(tokens.accessToken));
  }

  private readUserFromStorage(): CurrentUser | null {
    const token = typeof localStorage !== 'undefined' ? localStorage.getItem('rsaas.accessToken') : null;
    return token ? this.decodeUser(token) : null;
  }

  private decodeUser(accessToken: string): CurrentUser | null {
    const decoded = decodeAccessToken(accessToken);
    if (!decoded) return null;

    return {
      userId: decoded.sub,
      email: decoded.email,
      tenantId: decoded.tenant_id ?? null,
      locationId: decoded.location_id ?? null,
      isSuperAdmin: decoded.super_admin === 'true',
      roles: toStringArray(decoded.role),
      permissions: toStringArray(decoded.permission),
    };
  }

  private deviceInfo(): string {
    return typeof navigator !== 'undefined' ? navigator.userAgent : 'unknown';
  }
}
