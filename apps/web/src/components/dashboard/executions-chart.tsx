"use client";

import { useMemo } from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { ExecutionTimeseriesPoint } from "@/lib/executions/types";

interface Props {
  points: ExecutionTimeseriesPoint[];
  range: "24h" | "7d";
}

interface ChartDatum {
  label: string;
  succeeded: number;
  failed: number;
  total: number;
}

export function ExecutionsChart({ points, range }: Props) {
  const data = useMemo<ChartDatum[]>(
    () =>
      points.map((p) => {
        const d = new Date(p.bucket);
        const label =
          range === "24h"
            ? d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
            : `${d.toLocaleDateString([], { month: "short", day: "numeric" })} ${d.toLocaleTimeString([], { hour: "2-digit" })}`;
        return {
          label,
          succeeded: p.succeeded,
          failed: p.failed,
          total: p.succeeded + p.failed + p.running + p.pending,
        };
      }),
    [points, range],
  );

  const hasData = data.some((d) => d.total > 0);

  return (
    <div className="h-[260px] w-full">
      {hasData ? (
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
            <defs>
              <linearGradient id="grad-succeeded" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="hsl(160 84% 45%)" stopOpacity={0.45} />
                <stop offset="100%" stopColor="hsl(160 84% 45%)" stopOpacity={0} />
              </linearGradient>
              <linearGradient id="grad-failed" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="hsl(0 72% 60%)" stopOpacity={0.45} />
                <stop offset="100%" stopColor="hsl(0 72% 60%)" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid stroke="hsl(var(--border))" strokeDasharray="3 3" vertical={false} />
            <XAxis
              dataKey="label"
              stroke="hsl(var(--muted-foreground))"
              fontSize={11}
              tickLine={false}
              axisLine={false}
              minTickGap={24}
            />
            <YAxis
              stroke="hsl(var(--muted-foreground))"
              fontSize={11}
              tickLine={false}
              axisLine={false}
              allowDecimals={false}
              width={32}
            />
            <Tooltip
              cursor={{ stroke: "hsl(var(--border))" }}
              contentStyle={{
                background: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
                borderRadius: 6,
                fontSize: 12,
              }}
              labelStyle={{ color: "hsl(var(--muted-foreground))" }}
            />
            <Area
              type="monotone"
              dataKey="succeeded"
              stroke="hsl(160 84% 45%)"
              strokeWidth={1.5}
              fillOpacity={1}
              fill="url(#grad-succeeded)"
              stackId="1"
            />
            <Area
              type="monotone"
              dataKey="failed"
              stroke="hsl(0 72% 60%)"
              strokeWidth={1.5}
              fillOpacity={1}
              fill="url(#grad-failed)"
              stackId="1"
            />
          </AreaChart>
        </ResponsiveContainer>
      ) : (
        <div className="flex h-full items-center justify-center text-xs text-muted-foreground">
          No executions in the selected range.
        </div>
      )}
    </div>
  );
}
