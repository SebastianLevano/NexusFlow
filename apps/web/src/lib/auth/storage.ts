import type { AuthTokens } from "@/lib/api/types";

const KEY = "nexusflow.tokens";

export const tokenStorage = {
  get(): AuthTokens | null {
    if (typeof window === "undefined") return null;
    const raw = window.localStorage.getItem(KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthTokens;
    } catch {
      return null;
    }
  },
  set(tokens: AuthTokens) {
    if (typeof window === "undefined") return;
    window.localStorage.setItem(KEY, JSON.stringify(tokens));
  },
  clear() {
    if (typeof window === "undefined") return;
    window.localStorage.removeItem(KEY);
  },
};
