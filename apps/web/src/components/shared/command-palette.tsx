"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { Route } from "next";
import { Command } from "cmdk";
import { AnimatePresence, motion } from "framer-motion";
import {
  History,
  LayoutDashboard,
  LogOut,
  Moon,
  Plug,
  Plus,
  Sun,
  Workflow as WorkflowIcon,
  Zap,
} from "lucide-react";
import { useTheme } from "next-themes";
import { useAuthStore } from "@/stores/auth-store";
import { useCommandPalette } from "@/stores/command-palette-store";
import { workflowsApi } from "@/lib/workflows/api";
import { integrationsApi } from "@/lib/integrations/api";
import type { WorkflowSummary } from "@/lib/workflows/types";
import type { IntegrationSummary } from "@/lib/integrations/types";

export function CommandPalette() {
  const { isOpen, open, close, toggle } = useCommandPalette();
  const router = useRouter();
  const { setTheme, resolvedTheme } = useTheme();
  const logout = useAuthStore((s) => s.logout);

  const [workflows, setWorkflows] = useState<WorkflowSummary[]>([]);
  const [integrations, setIntegrations] = useState<IntegrationSummary[]>([]);

  // Global keyboard shortcut
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === "k" && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        toggle();
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [toggle]);

  // Lazy-load data when opening
  useEffect(() => {
    if (!isOpen) return;
    Promise.allSettled([workflowsApi.list(), integrationsApi.list()]).then(([w, i]) => {
      if (w.status === "fulfilled") setWorkflows(w.value);
      if (i.status === "fulfilled") setIntegrations(i.value);
    });
  }, [isOpen]);

  function go(href: string) {
    close();
    router.push(href as Route);
  }

  return (
    <AnimatePresence>
      {isOpen && (
        <Command.Dialog
          open={isOpen}
          onOpenChange={(v) => (v ? open() : close())}
          label="Command palette"
          shouldFilter
          className="fixed inset-0 z-50"
        >
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.12 }}
            className="fixed inset-0 bg-background/70 backdrop-blur-sm"
            onClick={close}
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.97, y: -8 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.97, y: -8 }}
            transition={{ duration: 0.15, ease: "easeOut" }}
            className="fixed left-1/2 top-[18%] w-[92%] max-w-xl -translate-x-1/2"
          >
            <div className="overflow-hidden rounded-xl border bg-card shadow-2xl">
              <Command.Input
                autoFocus
                placeholder="Type a command or search…"
                className="h-12 w-full border-b bg-transparent px-4 text-sm outline-none placeholder:text-muted-foreground"
              />
              <Command.List className="max-h-[60vh] overflow-y-auto p-1.5 text-sm">
                <Command.Empty className="px-3 py-6 text-center text-xs text-muted-foreground">
                  No results.
                </Command.Empty>

                <Group heading="Navigation">
                  <Item icon={<LayoutDashboard className="h-3.5 w-3.5" />} onSelect={() => go("/dashboard")}>
                    Go to Dashboard
                  </Item>
                  <Item icon={<WorkflowIcon className="h-3.5 w-3.5" />} onSelect={() => go("/workflows")}>
                    Go to Workflows
                  </Item>
                  <Item icon={<History className="h-3.5 w-3.5" />} onSelect={() => go("/executions")}>
                    Go to Executions
                  </Item>
                  <Item icon={<Plug className="h-3.5 w-3.5" />} onSelect={() => go("/integrations")}>
                    Go to Integrations
                  </Item>
                </Group>

                <Group heading="Actions">
                  <Item icon={<Plus className="h-3.5 w-3.5" />} onSelect={() => go("/workflows/new")}>
                    New workflow
                  </Item>
                  <Item
                    icon={resolvedTheme === "dark" ? <Sun className="h-3.5 w-3.5" /> : <Moon className="h-3.5 w-3.5" />}
                    onSelect={() => {
                      setTheme(resolvedTheme === "dark" ? "light" : "dark");
                      close();
                    }}
                  >
                    Toggle theme
                  </Item>
                  <Item
                    icon={<LogOut className="h-3.5 w-3.5" />}
                    onSelect={() => {
                      close();
                      void logout();
                    }}
                  >
                    Sign out
                  </Item>
                </Group>

                {workflows.length > 0 && (
                  <Group heading="Workflows">
                    {workflows.slice(0, 8).map((w) => (
                      <Item
                        key={w.id}
                        icon={<Zap className="h-3.5 w-3.5" />}
                        onSelect={() => go(`/workflows/${w.id}`)}
                        keywords={[w.name, w.description ?? "", w.triggerType]}
                      >
                        <span className="truncate">{w.name}</span>
                        <span className="ml-auto text-[10px] uppercase tracking-wide text-muted-foreground">
                          {w.isActive ? "Active" : "Inactive"}
                        </span>
                      </Item>
                    ))}
                  </Group>
                )}

                {integrations.length > 0 && (
                  <Group heading="Integrations">
                    {integrations.slice(0, 8).map((i) => (
                      <Item
                        key={i.id}
                        icon={<Plug className="h-3.5 w-3.5" />}
                        onSelect={() => go("/integrations")}
                        keywords={[i.label, i.provider]}
                      >
                        <span className="truncate">{i.label}</span>
                        <span className="ml-auto text-[10px] uppercase tracking-wide text-muted-foreground">
                          {i.provider}
                        </span>
                      </Item>
                    ))}
                  </Group>
                )}
              </Command.List>
              <div className="flex items-center justify-end gap-3 border-t px-3 py-2 text-[10px] text-muted-foreground">
                <Kbd>↑</Kbd>
                <Kbd>↓</Kbd>
                <span>navigate</span>
                <Kbd>↵</Kbd>
                <span>select</span>
                <Kbd>esc</Kbd>
                <span>close</span>
              </div>
            </div>
          </motion.div>
        </Command.Dialog>
      )}
    </AnimatePresence>
  );
}

function Group({ heading, children }: { heading: string; children: React.ReactNode }) {
  return (
    <Command.Group
      heading={heading}
      className="text-xs [&_[cmdk-group-heading]]:px-2.5 [&_[cmdk-group-heading]]:py-1.5 [&_[cmdk-group-heading]]:text-[10px] [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-muted-foreground"
    >
      {children}
    </Command.Group>
  );
}

function Item({
  icon,
  children,
  onSelect,
  keywords,
}: {
  icon: React.ReactNode;
  children: React.ReactNode;
  onSelect: () => void;
  keywords?: string[];
}) {
  return (
    <Command.Item
      onSelect={onSelect}
      keywords={keywords}
      className="flex cursor-pointer items-center gap-2 rounded-md px-2.5 py-2 text-sm text-foreground/90 transition-colors data-[selected=true]:bg-accent data-[selected=true]:text-foreground"
    >
      <span className="text-muted-foreground">{icon}</span>
      {children}
    </Command.Item>
  );
}

function Kbd({ children }: { children: React.ReactNode }) {
  return <kbd className="rounded border bg-background px-1.5 py-0.5 font-mono">{children}</kbd>;
}
