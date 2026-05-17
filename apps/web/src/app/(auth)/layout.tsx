import { Workflow } from "lucide-react";
import Link from "next/link";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="relative flex min-h-screen items-center justify-center px-4">
      <div className="pointer-events-none absolute inset-0 -z-10 [background:radial-gradient(60%_50%_at_50%_0%,hsl(var(--primary)/0.08),transparent)]" />
      <div className="w-full max-w-sm">
        <Link href="/" className="mb-10 flex items-center justify-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-md border bg-card">
            <Workflow className="h-4 w-4 text-primary" />
          </div>
          <span className="text-sm font-semibold tracking-tight">NexusFlow</span>
        </Link>
        {children}
      </div>
    </div>
  );
}
