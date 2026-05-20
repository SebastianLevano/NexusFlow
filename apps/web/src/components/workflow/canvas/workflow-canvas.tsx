"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Background,
  BackgroundVariant,
  Controls,
  MiniMap,
  ReactFlow,
  ReactFlowProvider,
  useEdgesState,
  useNodesState,
  type Edge,
  type Node,
  type NodeChange,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import { workflowsApi } from "@/lib/workflows/api";
import type {
  ActionType,
  TriggerType,
  Workflow,
  WorkflowLayout,
} from "@/lib/workflows/types";
import { nodeTypes, type StepNodeData, type TriggerNodeData } from "./nodes";

const TRIGGER_NODE_ID = "trigger";
const NODE_X = 80;
const NODE_GAP_Y = 140;
const NODE_TOP_Y = 40;

interface Props {
  workflow: Workflow;
  onLayoutSaved?: (workflow: Workflow) => void;
}

export function WorkflowCanvas(props: Props) {
  return (
    <ReactFlowProvider>
      <CanvasInner {...props} />
    </ReactFlowProvider>
  );
}

function CanvasInner({ workflow, onLayoutSaved }: Props) {
  const initial = useMemo(() => buildGraph(workflow), [workflow]);
  const [nodes, setNodes, onNodesChange] = useNodesState(initial.nodes);
  const [edges, , onEdgesChange] = useEdgesState(initial.edges);
  const [saving, setSaving] = useState(false);
  const saveTimer = useRef<number | null>(null);
  const lastSavedRef = useRef<string>("");

  useEffect(() => {
    setNodes(initial.nodes);
  }, [initial.nodes, setNodes]);

  const persist = useCallback(
    async (next: Node[]) => {
      const layout = nodesToLayout(next);
      const serialized = JSON.stringify(layout);
      if (serialized === lastSavedRef.current) return;
      lastSavedRef.current = serialized;
      setSaving(true);
      try {
        const updated = await workflowsApi.updateLayout(workflow.id, layout);
        onLayoutSaved?.(updated);
      } catch {
        toast.error("Could not save layout.");
      } finally {
        setSaving(false);
      }
    },
    [workflow.id, onLayoutSaved],
  );

  const handleNodesChange = useCallback(
    (changes: NodeChange[]) => {
      onNodesChange(changes);
      const hasPositionChange = changes.some((c) => c.type === "position" && c.dragging === false);
      if (!hasPositionChange) return;

      if (saveTimer.current) window.clearTimeout(saveTimer.current);
      saveTimer.current = window.setTimeout(() => {
        setNodes((current) => {
          void persist(current);
          return current;
        });
      }, 350);
    },
    [onNodesChange, persist, setNodes],
  );

  useEffect(() => {
    return () => {
      if (saveTimer.current) window.clearTimeout(saveTimer.current);
    };
  }, []);

  return (
    <div className="relative h-[560px] w-full overflow-hidden rounded-lg border bg-card">
      {saving && (
        <div className="absolute right-3 top-3 z-10 inline-flex items-center gap-1.5 rounded-md border bg-background px-2 py-1 text-[10px] text-muted-foreground">
          <Loader2 className="h-3 w-3 animate-spin" />
          Saving layout…
        </div>
      )}
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={handleNodesChange}
        onEdgesChange={onEdgesChange}
        nodeTypes={nodeTypes}
        fitView
        fitViewOptions={{ padding: 0.2, maxZoom: 1.1 }}
        proOptions={{ hideAttribution: true }}
        nodesConnectable={false}
        edgesFocusable={false}
        defaultEdgeOptions={{
          type: "smoothstep",
          animated: false,
          style: { stroke: "hsl(var(--border))", strokeWidth: 1.5 },
        }}
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} color="hsl(var(--border))" />
        <Controls
          position="bottom-right"
          showInteractive={false}
          className="!rounded-md !border !bg-card !shadow-sm"
        />
        <MiniMap
          pannable
          zoomable
          nodeStrokeWidth={2}
          maskColor="hsl(var(--background) / 0.6)"
          nodeColor={(n) =>
            n.type === "trigger" ? "hsl(var(--primary))" : "hsl(var(--muted-foreground))"
          }
          className="!rounded-md !border !bg-card"
        />
      </ReactFlow>
    </div>
  );
}

function buildGraph(workflow: Workflow): { nodes: Node[]; edges: Edge[] } {
  const positions = layoutPositions(workflow);

  const triggerData: TriggerNodeData = { triggerType: workflow.triggerType as TriggerType };
  const triggerNode: Node = {
    id: TRIGGER_NODE_ID,
    type: "trigger",
    position: positions[TRIGGER_NODE_ID] ?? { x: NODE_X, y: NODE_TOP_Y },
    data: triggerData as unknown as Record<string, unknown>,
    draggable: true,
  };

  const stepNodes: Node[] = workflow.steps.map((step, idx) => {
    const data: StepNodeData = {
      actionType: step.actionType as ActionType,
      orderIndex: step.orderIndex,
      configPreview: previewOf(step.config),
    };
    return {
      id: step.id,
      type: "step",
      position: positions[step.id] ?? { x: NODE_X, y: NODE_TOP_Y + (idx + 1) * NODE_GAP_Y },
      data: data as unknown as Record<string, unknown>,
      draggable: true,
    };
  });

  const edges: Edge[] = [];
  if (workflow.steps.length > 0) {
    edges.push({
      id: `${TRIGGER_NODE_ID}->${workflow.steps[0].id}`,
      source: TRIGGER_NODE_ID,
      target: workflow.steps[0].id,
    });
    for (let i = 0; i < workflow.steps.length - 1; i++) {
      edges.push({
        id: `${workflow.steps[i].id}->${workflow.steps[i + 1].id}`,
        source: workflow.steps[i].id,
        target: workflow.steps[i + 1].id,
      });
    }
  }

  return { nodes: [triggerNode, ...stepNodes], edges };
}

function layoutPositions(workflow: Workflow): Record<string, { x: number; y: number }> {
  const map: Record<string, { x: number; y: number }> = {};
  if (workflow.layout?.nodes) {
    for (const n of workflow.layout.nodes) {
      map[n.id] = { x: n.x, y: n.y };
    }
  }
  return map;
}

function nodesToLayout(nodes: Node[]): WorkflowLayout {
  return {
    nodes: nodes.map((n) => ({
      id: n.id,
      x: Math.round(n.position.x),
      y: Math.round(n.position.y),
    })),
  };
}

function previewOf(config: Record<string, unknown>): string {
  if (!config || Object.keys(config).length === 0) return "{}";
  const first = Object.entries(config)[0];
  if (!first) return "{}";
  const [k, v] = first;
  const valueStr = typeof v === "string" ? v : JSON.stringify(v);
  const compact = valueStr.length > 28 ? `${valueStr.slice(0, 28)}…` : valueStr;
  return `${k}: ${compact}`;
}
