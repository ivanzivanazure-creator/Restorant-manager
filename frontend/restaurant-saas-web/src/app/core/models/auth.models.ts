export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}

export interface LoginResult {
  requiresMfa: boolean;
  mfaChallengeToken: string | null;
  tokens: AuthTokens | null;
}

export interface RegisterOwnerRequest {
  companyName: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  packageName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  deviceInfo: string;
}

export interface VerifyMfaRequest {
  mfaChallengeToken: string;
  code: string;
  deviceInfo: string;
}

export interface MfaEnrollmentResult {
  secret: string;
  otpAuthUri: string;
  recoveryCodes: string[];
}

export interface DecodedAccessToken {
  sub: string;
  email: string;
  tenant_id?: string;
  location_id?: string;
  super_admin?: string;
  role: string | string[];
  permission: string | string[];
  exp: number;
}

export interface CurrentUser {
  userId: string;
  email: string;
  tenantId: string | null;
  locationId: string | null;
  isSuperAdmin: boolean;
  roles: string[];
  permissions: string[];
}
