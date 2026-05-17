"use client";

import { useState } from "react";
import { Copy, Loader2, MessageSquare, Send, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { integrationsApi } from "@/lib/integrations/api";
import {
  PROVIDER_LABELS,
  type IntegrationSummary,
} from "@/lib/integrations/types";

interface Props {
  integration: IntegrationSummary;
  onDeleted: () => void;
}

export function IntegrationCard({ integration, onDeleted }: Props) {
  const [busy, setBusy] = useState<"test" | "delete" | null>(null);

  async function handleTest() {
    setBusy("test");
    try {
      const result = await integrationsApi.test(integration.id);
      if (result.ok) toast.success(`Test message sent to ${integration.label}.`);
      else toast.error(result.error ?? `Test failed (${result.statusCode}).`);
    } catch {
      toast.error("Could not send test message.");
    } finally {
      setBusy(null);
    }
  }

  async function handleDelete() {
    if (!confirm(`Disconnect "${integration.label}"? Workflows referencing it will fail.`)) return;
    setBusy("delete");
    try {
      await integrationsApi.remove(integration.id);
      toast.success("Integration removed.");
      onDeleted();
    } catch {
      toast.error("Could not remove integration.");
      setBusy(null);
    }
  }

  async function copyId() {
    try {
      await navigator.clipboard.writeText(integration.id);
      toast.success("Integration ID copied.");
    } catch {
      // ignore
    }
  }

  return (
    <div className="flex items-center gap-4 rounded-lg border bg-card p-4">
      <ProviderIcon provider={integration.provider} />
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="truncate text-sm font-medium">{integration.label}</p>
          <span className="rounded-md border bg-background px-1.5 py-0.5 text-[10px] uppercase tracking-wide text-muted-foreground">
            {PROVIDER_LABELS[integration.provider]}
          </span>
        </div>
        <button
          type="button"
          onClick={copyId}
          title="Click to copy"
          className="mt-0.5 flex items-center gap-1 truncate text-xs text-muted-foreground hover:text-foreground"
        >
          <span className="font-mono">{integration.id}</span>
          <Copy className="h-3 w-3" />
        </button>
      </div>
      <Button variant="outline" size="sm" onClick={handleTest} disabled={busy !== null}>
        {busy === "test" ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Send className="h-3.5 w-3.5" />}
        Send test
      </Button>
      <Button variant="ghost" size="sm" onClick={handleDelete} disabled={busy !== null} aria-label="Disconnect">
        {busy === "delete" ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Trash2 className="h-3.5 w-3.5" />}
      </Button>
    </div>
  );
}

function ProviderIcon({ provider }: { provider: IntegrationSummary["provider"] }) {
  const tone =
    provider === "slack"
      ? "bg-purple-500/10 text-purple-300 border-purple-500/20"
      : "bg-indigo-500/10 text-indigo-300 border-indigo-500/20";
  return (
    <div className={`flex h-9 w-9 items-center justify-center rounded-md border ${tone}`}>
      <MessageSquare className="h-4 w-4" />
    </div>
  );
}
