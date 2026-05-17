"use client";

import { useCallback, useEffect, useState } from "react";
import { MessageSquare, Plug, Plus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { ListSkeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { ConnectForm } from "@/components/integrations/connect-form";
import { IntegrationCard } from "@/components/integrations/integration-card";
import { integrationsApi } from "@/lib/integrations/api";
import type { IntegrationProvider, IntegrationSummary } from "@/lib/integrations/types";

export default function IntegrationsPage() {
  const [items, setItems] = useState<IntegrationSummary[] | null>(null);
  const [connecting, setConnecting] = useState<IntegrationProvider | null>(null);

  const load = useCallback(async () => {
    try {
      setItems(await integrationsApi.list());
    } catch {
      toast.error("Could not load integrations.");
      setItems([]);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  function handleCreated(integration: IntegrationSummary) {
    setItems((prev) => [integration, ...(prev ?? [])]);
    setConnecting(null);
  }

  return (
    <div className="space-y-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Integrations</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Connect Slack or Discord via incoming webhooks. Use them from workflow steps with{" "}
            <code className="rounded bg-muted px-1 py-0.5 font-mono text-xs">slack_post_message</code> or{" "}
            <code className="rounded bg-muted px-1 py-0.5 font-mono text-xs">discord_post_message</code>.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            onClick={() => setConnecting("slack")}
            disabled={connecting === "slack"}
          >
            <Plus className="h-3.5 w-3.5" />
            Connect Slack
          </Button>
          <Button
            variant="outline"
            onClick={() => setConnecting("discord")}
            disabled={connecting === "discord"}
          >
            <Plus className="h-3.5 w-3.5" />
            Connect Discord
          </Button>
        </div>
      </header>

      {connecting && (
        <ConnectForm
          provider={connecting}
          onCreated={handleCreated}
          onCancel={() => setConnecting(null)}
        />
      )}

      {items === null ? (
        <ListSkeleton rows={2} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={<Plug className="h-4 w-4" />}
          title="No integrations yet"
          description="Connect Slack or Discord to start posting messages from your workflows."
          action={
            <div className="flex items-center gap-2">
              <Button variant="outline" onClick={() => setConnecting("slack")}>
                <MessageSquare className="h-3.5 w-3.5" />
                Connect Slack
              </Button>
              <Button variant="outline" onClick={() => setConnecting("discord")}>
                <MessageSquare className="h-3.5 w-3.5" />
                Connect Discord
              </Button>
            </div>
          }
        />
      ) : (
        <ul className="space-y-2">
          {items.map((i) => (
            <li key={i.id}>
              <IntegrationCard integration={i} onDeleted={load} />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
