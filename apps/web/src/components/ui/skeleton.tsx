import { cn } from "@/lib/utils";

export function Skeleton({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("animate-pulse rounded-md bg-muted/40", className)}
      aria-hidden
      {...props}
    />
  );
}

export function RowSkeleton({ height = 60 }: { height?: number }) {
  return (
    <div className="flex items-center gap-4 rounded-lg border bg-card p-4" style={{ minHeight: height }}>
      <Skeleton className="h-9 w-9 shrink-0 rounded-md" />
      <div className="flex-1 space-y-2">
        <Skeleton className="h-3.5 w-1/3" />
        <Skeleton className="h-3 w-2/3" />
      </div>
    </div>
  );
}

export function ListSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <ul className="space-y-2">
      {Array.from({ length: rows }).map((_, i) => (
        <li key={i}>
          <RowSkeleton />
        </li>
      ))}
    </ul>
  );
}

export function StatCardSkeleton() {
  return (
    <div className="space-y-3 rounded-lg border bg-card p-5">
      <Skeleton className="h-3 w-1/3" />
      <Skeleton className="h-7 w-1/2" />
      <Skeleton className="h-2.5 w-1/3" />
    </div>
  );
}
