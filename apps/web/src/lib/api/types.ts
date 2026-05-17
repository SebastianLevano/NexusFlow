export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  tokens: AuthTokens;
}

export interface CurrentUser {
  userId: string;
  email: string;
  createdAt: string;
}

export interface ApiError {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
}
