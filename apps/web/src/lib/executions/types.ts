export type ExecutionStatus = "pending" | "running" | "succeeded" | "failed";
export type StepStatus = "pending" | "running" | "succeeded" | "failed" | "skipped";
export type TriggerSource = "webhook" | "schedule" | "manual";

export interface ExecutionSummary {
  id: string;
  workflowId: string;
  status: ExecutionStatus;
  triggeredBy: TriggerSource;
  createdAt: string;
  startedAt: string | null;
  finishedAt: string | null;
  durationMs: number | null;
  errorMessage: string | null;
  stepCount: number;
}

export interface StepExecution {
  id: string;
  stepId: string;
  orderIndex: number;
  actionType: string;
  status: StepStatus;
  input: Record<string, unknown>;
  output: Record<string, unknown>;
  error: string | null;
  startedAt: string | null;
  finishedAt: string | null;
  durationMs: number | null;
}

export interface Execution {
  id: string;
  workflowId: string;
  status: ExecutionStatus;
  triggeredBy: TriggerSource;
  triggerPayload: Record<string, unknown>;
  createdAt: string;
  startedAt: string | null;
  finishedAt: string | null;
  durationMs: number | null;
  errorMessage: string | null;
  steps: StepExecution[];
}

export const STATUS_LABELS: Record<ExecutionStatus, string> = {
  pending: "Pending",
  running: "Running",
  succeeded: "Succeeded",
  failed: "Failed",
};

export interface ExecutionStats {
  workflowsActive: number;
  workflowsTotal: number;
  runs24h: number;
  runs7d: number;
  succeededLast24h: number;
  failedLast24h: number;
  successRateLast24h: number;
  avgDurationMsLast24h: number | null;
  p50DurationMsLast24h: number | null;
  p95DurationMsLast24h: number | null;
}

export interface ExecutionTimeseriesPoint {
  bucket: string;
  succeeded: number;
  failed: number;
  running: number;
  pending: number;
}

export interface ExecutionTimeseriesResponse {
  range: "24h" | "7d";
  interval: string;
  points: ExecutionTimeseriesPoint[];
}

export type ExecutionsListFilters = {
  workflowId?: string;
  status?: ExecutionStatus;
  range?: "1h" | "24h" | "7d" | "30d";
};
