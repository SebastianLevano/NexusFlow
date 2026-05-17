import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from "axios";
import { tokenStorage } from "@/lib/auth/storage";
import type { AuthResponse } from "./types";

const baseURL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

interface RetryableConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
  _skipAuth?: boolean;
}

export const api: AxiosInstance = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" },
});

api.interceptors.request.use((config) => {
  const cfg = config as RetryableConfig;
  if (cfg._skipAuth) return cfg;
  const tokens = tokenStorage.get();
  if (tokens?.accessToken) {
    cfg.headers.set("Authorization", `Bearer ${tokens.accessToken}`);
  }
  return cfg;
});

let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const tokens = tokenStorage.get();
  if (!tokens?.refreshToken) return null;

  try {
    const { data } = await axios.post<AuthResponse>(
      `${baseURL}/auth/refresh`,
      { refreshToken: tokens.refreshToken },
      { headers: { "Content-Type": "application/json" } },
    );
    tokenStorage.set(data.tokens);
    return data.tokens.accessToken;
  } catch {
    tokenStorage.clear();
    return null;
  }
}

api.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as RetryableConfig | undefined;
    const status = error.response?.status;

    if (status !== 401 || !original || original._retry || original._skipAuth) {
      return Promise.reject(error);
    }

    original._retry = true;
    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null;
    });

    const newToken = await refreshPromise;
    if (!newToken) {
      if (typeof window !== "undefined") window.location.href = "/login";
      return Promise.reject(error);
    }

    original.headers.set("Authorization", `Bearer ${newToken}`);
    return api(original);
  },
);

export const authApi = {
  register: (email: string, password: string) =>
    api.post<AuthResponse>("/auth/register", { email, password }, { _skipAuth: true } as RetryableConfig).then((r) => r.data),
  login: (email: string, password: string) =>
    api.post<AuthResponse>("/auth/login", { email, password }, { _skipAuth: true } as RetryableConfig).then((r) => r.data),
  logout: (refreshToken: string) =>
    api.post("/auth/logout", { refreshToken }, { _skipAuth: true } as RetryableConfig),
  me: () => api.get<{ userId: string; email: string; createdAt: string }>("/auth/me").then((r) => r.data),
};
