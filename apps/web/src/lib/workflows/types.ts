export type TriggerType = "webhook" | "schedule";

export type ActionType =
  | "http_request"
  | "save_to_database"
  | "send_notification"
  | "slack_post_message"
  | "discord_post_message";

export interface WorkflowSummary {
  id: string;
  name: string;
  description: string | null;
  triggerType: TriggerType;
  isActive: boolean;
  stepCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface WorkflowStep {
  id: string;
  orderIndex: number;
  actionType: ActionType;
  config: Record<string, unknown>;
}

export interface Workflow {
  id: string;
  name: string;
  description: string | null;
  triggerType: TriggerType;
  triggerConfig: Record<string, unknown>;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  steps: WorkflowStep[];
}

export interface StepInput {
  orderIndex: number;
  actionType: ActionType;
  config: Record<string, unknown>;
}

export interface WorkflowInput {
  name: string;
  description: string | null;
  triggerType: TriggerType;
  triggerConfig: Record<string, unknown>;
  steps: StepInput[];
}

export const TRIGGER_LABELS: Record<TriggerType, string> = {
  webhook: "Webhook",
  schedule: "Schedule",
};

export const ACTION_LABELS: Record<ActionType, string> = {
  http_request: "HTTP request",
  save_to_database: "Save to database",
  send_notification: "Send notification",
  slack_post_message: "Slack message",
  discord_post_message: "Discord message",
};
