import { z } from "zod";
import type { ActionType, TriggerType } from "./types";

const triggerTypeSchema = z.enum(["webhook", "schedule"]);
const actionTypeSchema = z.enum([
  "http_request",
  "save_to_database",
  "send_notification",
  "slack_post_message",
  "discord_post_message",
]);

const jsonObjectString = z
  .string()
  .transform((value, ctx) => {
    const trimmed = value.trim();
    if (trimmed.length === 0) return {} as Record<string, unknown>;
    try {
      const parsed = JSON.parse(trimmed) as unknown;
      if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Must be a JSON object." });
        return z.NEVER;
      }
      return parsed as Record<string, unknown>;
    } catch {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Invalid JSON." });
      return z.NEVER;
    }
  });

export const stepFormSchema = z.object({
  actionType: actionTypeSchema,
  configRaw: jsonObjectString,
});

export const workflowFormSchema = z.object({
  name: z.string().min(1, "Name is required.").max(200),
  description: z.string().max(2000).optional().default(""),
  triggerType: triggerTypeSchema,
  triggerConfigRaw: jsonObjectString,
  steps: z.array(stepFormSchema),
});

export type WorkflowFormInput = z.input<typeof workflowFormSchema>;
export type WorkflowFormOutput = z.output<typeof workflowFormSchema>;

export const TRIGGER_OPTIONS: { value: TriggerType; label: string; sample: string }[] = [
  { value: "webhook", label: "Webhook", sample: '{\n  "secret": "shared-secret"\n}' },
  { value: "schedule", label: "Schedule", sample: '{\n  "cron": "*/5 * * * *"\n}' },
];

export const ACTION_OPTIONS: { value: ActionType; label: string; sample: string }[] = [
  {
    value: "http_request",
    label: "HTTP request",
    sample: '{\n  "method": "POST",\n  "url": "https://example.com",\n  "body": {}\n}',
  },
  { value: "save_to_database", label: "Save to database", sample: "{}" },
  {
    value: "send_notification",
    label: "Send notification",
    sample: '{\n  "message": "Hello from NexusFlow"\n}',
  },
  {
    value: "slack_post_message",
    label: "Slack message",
    sample: '{\n  "integrationId": "<paste from /integrations>",\n  "text": "Hello from NexusFlow"\n}',
  },
  {
    value: "discord_post_message",
    label: "Discord message",
    sample: '{\n  "integrationId": "<paste from /integrations>",\n  "text": "Hello from NexusFlow"\n}',
  },
];
