import { api } from "@/lib/api/client";
import type {
  Execution,
  ExecutionStats,
  ExecutionSummary,
  ExecutionTimeseriesResponse,
  ExecutionsListFilters,
} from "./types";

export const executionsApi = {
  list: (filters: ExecutionsListFilters = {}) => {
    const params: Record<string, string> = {};
    if (filters.workflowId) params.workflowId = filters.workflowId;
    if (filters.status) params.status = filters.status;
    if (filters.range) params.range = filters.range;
    return api
      .get<ExecutionSummary[]>("/executions", { params: Object.keys(params).length ? params : undefined })
      .then((r) => r.data);
  },
  get: (id: string) => api.get<Execution>(`/executions/${id}`).then((r) => r.data),
  stats: () => api.get<ExecutionStats>("/executions/stats").then((r) => r.data),
  timeseries: (range: "24h" | "7d" = "24h") =>
    api
      .get<ExecutionTimeseriesResponse>("/executions/timeseries", { params: { range } })
      .then((r) => r.data),
  runManual: (workflowId: string, payload?: Record<string, unknown>) =>
    api
      .post<{ executionId: string }>(`/workflows/${workflowId}/runs`, payload ?? {})
      .then((r) => r.data),
};
