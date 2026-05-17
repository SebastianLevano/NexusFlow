"use client";

import { create } from "zustand";
import { authApi } from "@/lib/api/client";
import type { AuthResponse, CurrentUser } from "@/lib/api/types";
import { tokenStorage } from "@/lib/auth/storage";

interface AuthState {
  user: CurrentUser | null;
  status: "idle" | "loading" | "authenticated" | "unauthenticated";
  error: string | null;
  register: (email: string, password: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hydrate: () => Promise<void>;
}

function applyAuth(response: AuthResponse, set: (s: Partial<AuthState>) => void) {
  tokenStorage.set(response.tokens);
  set({
    user: { userId: response.userId, email: response.email, createdAt: new Date().toISOString() },
    status: "authenticated",
    error: null,
  });
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  status: "idle",
  error: null,

  async register(email, password) {
    set({ status: "loading", error: null });
    try {
      const res = await authApi.register(email, password);
      applyAuth(res, set);
    } catch (err: unknown) {
      set({ status: "unauthenticated", error: extractMessage(err, "Could not register.") });
      throw err;
    }
  },

  async login(email, password) {
    set({ status: "loading", error: null });
    try {
      const res = await authApi.login(email, password);
      applyAuth(res, set);
    } catch (err: unknown) {
      set({ status: "unauthenticated", error: extractMessage(err, "Invalid credentials.") });
      throw err;
    }
  },

  async logout() {
    const tokens = tokenStorage.get();
    try {
      if (tokens?.refreshToken) await authApi.logout(tokens.refreshToken);
    } catch {
      // swallow — local state is what matters for UX
    }
    tokenStorage.clear();
    set({ user: null, status: "unauthenticated", error: null });
  },

  async hydrate() {
    const tokens = tokenStorage.get();
    if (!tokens?.accessToken) {
      set({ status: "unauthenticated" });
      return;
    }
    set({ status: "loading" });
    try {
      const me = await authApi.me();
      set({ user: me, status: "authenticated" });
    } catch {
      tokenStorage.clear();
      set({ user: null, status: "unauthenticated" });
    }
  },
}));

function extractMessage(err: unknown, fallback: string): string {
  if (typeof err === "object" && err !== null && "response" in err) {
    const data = (err as { response?: { data?: { detail?: string; title?: string } } }).response?.data;
    return data?.detail ?? data?.title ?? fallback;
  }
  return fallback;
}
