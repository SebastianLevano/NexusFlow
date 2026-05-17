"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Command,
  History,
  LayoutDashboard,
  LogOut,
  Plug,
  Workflow as WorkflowIcon,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";
import { useCommandPalette } from "@/stores/command-palette-store";
import { Button } from "@/components/ui/button";
import { ThemeToggle } from "@/components/shared/theme-toggle";

const nav = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/workflows", label: "Workflows", icon: WorkflowIcon },
  { href: "/executions", label: "Executions", icon: History },
  { href: "/integrations", label: "Integrations", icon: Plug },
] as const;

export function Sidebar() {
  const pathname = usePathname();
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  const openPalette = useCommandPalette((s) => s.open);

  return (
    <aside className="flex h-screen w-60 flex-col border-r bg-card/50">
      <div className="flex h-14 items-center gap-2 border-b px-4">
        <div className="flex h-6 w-6 items-center justify-center rounded-md border bg-background">
          <WorkflowIcon className="h-3.5 w-3.5 text-primary" />
        </div>
        <span className="text-sm font-semibold tracking-tight">NexusFlow</span>
      </div>

      <div className="border-b p-2">
        <button
          type="button"
          onClick={openPalette}
          className="flex w-full items-center gap-2 rounded-md border bg-background px-2.5 py-1.5 text-xs text-muted-foreground transition-colors hover:text-foreground"
        >
          <Command className="h-3 w-3" />
          <span className="flex-1 text-left">Quick search</span>
          <kbd className="rounded border bg-card px-1 py-0.5 font-mono text-[10px]">⌘K</kbd>
        </button>
      </div>

      <nav className="flex-1 space-y-0.5 p-2">
        {nav.map((item) => {
          const active = pathname === item.href || pathname.startsWith(`${item.href}/`);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-2 rounded-md px-2.5 py-1.5 text-sm transition-colors",
                active
                  ? "bg-accent text-foreground"
                  : "text-muted-foreground hover:bg-accent/50 hover:text-foreground",
              )}
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="border-t p-3">
        <div className="mb-2 flex items-center gap-2 rounded-md px-2 py-1.5">
          <div className="flex h-7 w-7 items-center justify-center rounded-full border bg-background text-xs font-medium">
            {user?.email?.[0]?.toUpperCase() ?? "?"}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-xs font-medium">{user?.email ?? ""}</p>
          </div>
        </div>
        <ThemeToggle />
        <Button
          variant="ghost"
          size="sm"
          className="w-full justify-start text-muted-foreground"
          onClick={() => void logout()}
        >
          <LogOut className="h-3.5 w-3.5" />
          Sign out
        </Button>
      </div>
    </aside>
  );
}
