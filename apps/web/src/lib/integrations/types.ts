export type IntegrationProvider = "slack" | "discord";

export interface IntegrationSummary {
  id: string;
  provider: IntegrationProvider;
  label: string;
  createdAt: string;
  lastUsedAt: string | null;
}

export interface CreateIntegrationInput {
  provider: IntegrationProvider;
  label: string;
  webhookUrl: string;
}

export interface TestMessageResponse {
  ok: boolean;
  statusCode: number;
  error: string | null;
}

export const PROVIDER_LABELS: Record<IntegrationProvider, string> = {
  slack: "Slack",
  discord: "Discord",
};
