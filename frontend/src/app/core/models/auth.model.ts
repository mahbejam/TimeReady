/** Roles issued by the backend. */
export type Role = 'Admin' | 'Operator';

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  roles: Role[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

/** Response of POST /api/auth/login and /api/auth/refresh. */
export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: AuthUser;
}
