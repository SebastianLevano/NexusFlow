import type { ReactNode } from "react";

export function ComingSoon({
  icon,
  title,
  description,
  phase,
}: {
  icon: ReactNode;
  title: string;
  description: string;
  phase: string;
}) {
  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{description}</p>
      </header>

      <div className="flex flex-col items-center justify-center rounded-lg border border-dashed bg-card/30 px-6 py-20 text-center">
        <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-md border bg-card text-muted-foreground">
          {icon}
        </div>
        <p className="text-sm font-medium">Coming in {phase}</p>
        <p className="mt-1 max-w-sm text-sm text-muted-foreground">
          This area is part of an upcoming phase. The plan is in <code className="font-mono text-xs">docs/PLAN.md</code>.
        </p>
      </div>
    </div>
  );
}
