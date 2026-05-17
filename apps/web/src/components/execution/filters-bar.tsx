"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import type { Route } from "next";
import { X } from "lucide-react";
import { workflowsApi } from "@/lib/workflows/api";
import type { WorkflowSummary } from "@/lib/workflows/types";
import type { ExecutionStatus } from "@/lib/executions/types";

const STATUSES: { value: ExecutionStatus | ""; label: string }[] = [
  { value: "", label: "Any status" },
  { value: "pending", label: "Pending" },
  { value: "running", label: "Running" },
  { value: "succeeded", label: "Succeeded" },
  { value: "failed", label: "Failed" },
];

const RANGES: { value: "" | "1h" | "24h" | "7d" | "30d"; label: string }[] = [
  { value: "", label: "All time" },
  { value: "1h", label: "Last hour" },
  { value: "24h", label: "Last 24h" },
  { value: "7d", label: "Last 7 days" },
  { value: "30d", label: "Last 30 days" },
];

export function FiltersBar() {
  const router = useRouter();
  const params = useSearchParams();
  const workflowId = params.get("workflowId") ?? "";
  const status = params.get("status") ?? "";
  const range = params.get("range") ?? "";

  const [workflows, setWorkflows] = useState<WorkflowSummary[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    workflowsApi.list().then(
      (list) => {
        if (!cancelled) setWorkflows(list);
      },
      () => {
        if (!cancelled) setWorkflows([]);
      },
    );
    return () => {
      cancelled = true;
    };
  }, []);

  function update(key: string, value: string) {
    const next = new URLSearchParams(params.toString());
    if (value) next.set(key, value);
    else next.delete(key);
    router.replace(`/executions${next.size ? `?${next.toString()}` : ""}` as Route);
  }

  function clearAll() {
    router.replace("/executions");
  }

  const hasAny = Boolean(workflowId || status || range);

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Select
        ariaLabel="Workflow"
        value={workflowId}
        onChange={(v) => update("workflowId", v)}
        options={[
          { value: "", label: "All workflows" },
          ...(workflows?.map((w) => ({ value: w.id, label: w.name })) ?? []),
        ]}
        disabled={workflows === null}
      />
      <Select
        ariaLabel="Status"
        value={status}
        onChange={(v) => update("status", v)}
        options={STATUSES}
      />
      <Select
        ariaLabel="Time range"
        value={range}
        onChange={(v) => update("range", v)}
        options={RANGES}
      />
      {hasAny && (
        <button
          type="button"
          onClick={clearAll}
          className="inline-flex items-center gap-1 rounded-md border bg-card px-2.5 py-1.5 text-xs text-muted-foreground hover:text-foreground"
        >
          <X className="h-3 w-3" />
          Clear
        </button>
      )}
    </div>
  );
}

interface SelectProps {
  ariaLabel: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  disabled?: boolean;
}

function Select({ ariaLabel, value, onChange, options, disabled }: SelectProps) {
  return (
    <select
      aria-label={ariaLabel}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className="h-8 cursor-pointer rounded-md border bg-card px-2.5 text-xs text-foreground transition-colors hover:border-border/80 focus:outline-none focus:ring-1 focus:ring-ring disabled:opacity-50"
    >
      {options.map((o) => (
        <option key={o.value} value={o.value} className="bg-background">
          {o.label}
        </option>
      ))}
    </select>
  );
}
