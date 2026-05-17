import { api } from "@/lib/api/client";
import type {
  CreateIntegrationInput,
  IntegrationSummary,
  TestMessageResponse,
} from "./types";

export const integrationsApi = {
  list: () => api.get<IntegrationSummary[]>("/integrations").then((r) => r.data),
  create: (input: CreateIntegrationInput) =>
    api.post<IntegrationSummary>("/integrations", input).then((r) => r.data),
  remove: (id: string) => api.delete(`/integrations/${id}`).then(() => undefined),
  test: (id: string, text?: string) =>
    api
      .post<TestMessageResponse>(`/integrations/${id}/test`, { text: text ?? null })
      .then((r) => r.data),
};
