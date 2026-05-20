import { api } from "@/lib/api/client";
import type { Workflow, WorkflowInput, WorkflowSummary } from "./types";

export const workflowsApi = {
  list: () => api.get<WorkflowSummary[]>("/workflows").then((r) => r.data),
  get: (id: string) => api.get<Workflow>(`/workflows/${id}`).then((r) => r.data),
  create: (input: WorkflowInput) => api.post<Workflow>("/workflows", input).then((r) => r.data),
  update: (id: string, input: WorkflowInput) =>
    api.put<Workflow>(`/workflows/${id}`, input).then((r) => r.data),
  remove: (id: string) => api.delete(`/workflows/${id}`).then(() => undefined),
  activate: (id: string) => api.post<Workflow>(`/workflows/${id}/activate`).then((r) => r.data),
  deactivate: (id: string) => api.post<Workflow>(`/workflows/${id}/deactivate`).then((r) => r.data),
  updateLayout: (id: string, layout: import("./types").WorkflowLayout) =>
    api.put<Workflow>(`/workflows/${id}/layout`, { layout }).then((r) => r.data),
};
