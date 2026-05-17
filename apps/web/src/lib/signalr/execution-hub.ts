"use client";

import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { tokenStorage } from "@/lib/auth/storage";

export interface ExecutionLiveEvent {
  executionId: string;
  status: "pending" | "running" | "succeeded" | "failed";
  at: string;
  durationMs: number | null;
  errorMessage: string | null;
}

export interface StepLiveEvent {
  executionId: string;
  stepId: string;
  orderIndex: number;
  actionType: string;
  status: "pending" | "running" | "succeeded" | "failed" | "skipped";
  at: string;
  durationMs: number | null;
  error: string | null;
  inputJson: string | null;
  outputJson: string | null;
}

export interface ExecutionHubHandlers {
  onExecution: (evt: ExecutionLiveEvent) => void;
  onStep: (evt: StepLiveEvent) => void;
  onStatusChange?: (state: HubConnectionState) => void;
}

const baseURL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

export function buildExecutionHub(handlers: ExecutionHubHandlers): HubConnection {
  const url = `${baseURL}/hubs/executions`;

  const connection = new HubConnectionBuilder()
    .withUrl(url, {
      accessTokenFactory: () => tokenStorage.get()?.accessToken ?? "",
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 15000])
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on("ExecutionState", (evt: ExecutionLiveEvent) => handlers.onExecution(evt));
  connection.on("StepState", (evt: StepLiveEvent) => handlers.onStep(evt));

  if (handlers.onStatusChange) {
    const notify = () => handlers.onStatusChange?.(connection.state);
    connection.onreconnecting(notify);
    connection.onreconnected(notify);
    connection.onclose(notify);
  }

  return connection;
}

export async function joinExecution(connection: HubConnection, executionId: string): Promise<boolean> {
  if (connection.state !== HubConnectionState.Connected) return false;
  try {
    const result = await connection.invoke<{ ok: boolean; reason: string | null }>("Join", executionId);
    return Boolean(result?.ok);
  } catch {
    return false;
  }
}

export async function leaveExecution(connection: HubConnection, executionId: string): Promise<void> {
  if (connection.state !== HubConnectionState.Connected) return;
  try {
    await connection.invoke("Leave", executionId);
  } catch {
    // best-effort
  }
}

export { HubConnectionState };
