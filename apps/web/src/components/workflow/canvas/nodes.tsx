"use client";

import { memo } from "react";
import { Handle, Position, type NodeProps } from "@xyflow/react";
import {
  Code2,
  Database,
  Globe,
  MessageSquare,
  Send,
  Webhook,
  Workflow as WorkflowIcon,
  Zap,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { ACTION_LABELS, TRIGGER_LABELS, type ActionType, type TriggerType } from "@/lib/workflows/types";

export interface TriggerNodeData extends Record<string, unknown> {
  triggerType: TriggerType;
}

export interface StepNodeData extends Record<string, unknown> {
  actionType: ActionType;
  orderIndex: number;
  configPreview: string;
}

const TRIGGER_ICONS: Record<TriggerType, React.ComponentType<{ className?: string }>> = {
  webhook: Webhook,
  schedule: Code2,
};

const ACTION_ICONS: Record<ActionType, React.ComponentType<{ className?: string }>> = {
  http_request: Globe,
  save_to_database: Database,
  send_notification: Send,
  slack_post_message: MessageSquare,
  discord_post_message: MessageSquare,
};

const ACTION_TONES: Record<ActionType, string> = {
  http_request: "bg-sky-500/10 text-sky-300 border-sky-500/30",
  save_to_database: "bg-amber-500/10 text-amber-300 border-amber-500/30",
  send_notification: "bg-emerald-500/10 text-emerald-300 border-emerald-500/30",
  slack_post_message: "bg-purple-500/10 text-purple-300 border-purple-500/30",
  discord_post_message: "bg-indigo-500/10 text-indigo-300 border-indigo-500/30",
};

export const TriggerNode = memo(function TriggerNode({ data }: NodeProps) {
  const d = data as TriggerNodeData;
  const Icon = TRIGGER_ICONS[d.triggerType] ?? Zap;
  return (
    <div className="w-56 rounded-lg border border-primary/40 bg-card shadow-lg">
      <div className="flex items-center gap-2 border-b border-primary/20 bg-primary/5 px-3 py-2">
        <div className="flex h-6 w-6 items-center justify-center rounded-md border border-primary/30 bg-background text-primary">
          <Icon className="h-3 w-3" />
        </div>
        <span className="text-[10px] font-medium uppercase tracking-wider text-primary">Trigger</span>
      </div>
      <div className="px-3 py-2.5">
        <p className="text-sm font-medium">{TRIGGER_LABELS[d.triggerType]}</p>
        <p className="mt-0.5 text-xs text-muted-foreground">Entry point of the workflow</p>
      </div>
      <Handle type="source" position={Position.Bottom} className="!h-2 !w-2 !border !border-primary !bg-background" />
    </div>
  );
});

export const StepNode = memo(function StepNode({ data, selected }: NodeProps) {
  const d = data as StepNodeData;
  const Icon = ACTION_ICONS[d.actionType] ?? WorkflowIcon;
  const tone = ACTION_TONES[d.actionType] ?? "bg-muted text-foreground border-border";
  return (
    <div
      className={cn(
        "w-56 rounded-lg border bg-card shadow-md transition-shadow",
        selected ? "border-foreground/60 ring-1 ring-foreground/40" : "border-border",
      )}
    >
      <Handle type="target" position={Position.Top} className="!h-2 !w-2 !border !border-foreground/40 !bg-background" />
      <div className="flex items-center gap-2 border-b px-3 py-2">
        <div className={cn("flex h-6 w-6 items-center justify-center rounded-md border", tone)}>
          <Icon className="h-3 w-3" />
        </div>
        <span className="text-[10px] font-medium uppercase tracking-wider text-muted-foreground">
          Step {d.orderIndex + 1}
        </span>
      </div>
      <div className="px-3 py-2.5">
        <p className="text-sm font-medium">{ACTION_LABELS[d.actionType]}</p>
        <p className="mt-0.5 truncate font-mono text-[10px] text-muted-foreground">{d.configPreview}</p>
      </div>
      <Handle type="source" position={Position.Bottom} className="!h-2 !w-2 !border !border-foreground/40 !bg-background" />
    </div>
  );
});

export const nodeTypes = {
  trigger: TriggerNode,
  step: StepNode,
};
