"use client";

import { useState } from "react";
import { Loader2, X } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { integrationsApi } from "@/lib/integrations/api";
import { PROVIDER_LABELS, type IntegrationProvider, type IntegrationSummary } from "@/lib/integrations/types";

interface Props {
  provider: IntegrationProvider;
  onCreated: (integration: IntegrationSummary) => void;
  onCancel: () => void;
}

const HINTS: Record<IntegrationProvider, string> = {
  slack: "Create an Incoming Webhook at https://api.slack.com/apps and paste the URL (starts with https://hooks.slack.com/services/…).",
  discord: "Open your channel → Edit Channel → Integrations → Webhooks → New Webhook → Copy URL (starts with https://discord.com/api/webhooks/…).",
};

export function ConnectForm({ provider, onCreated, onCancel }: Props) {
  const [label, setLabel] = useState(`${PROVIDER_LABELS[provider]} channel`);
  const [webhookUrl, setWebhookUrl] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    try {
      const created = await integrationsApi.create({ provider, label, webhookUrl });
      toast.success(`Connected ${PROVIDER_LABELS[provider]}.`);
      onCreated(created);
    } catch (err) {
      const reason =
        (err as { response?: { data?: { detail?: string } } }).response?.data?.detail ??
        "Could not connect integration.";
      toast.error(reason);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} className="space-y-4 rounded-lg border bg-card p-5">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Connect {PROVIDER_LABELS[provider]}</h3>
        <button
          type="button"
          onClick={onCancel}
          aria-label="Cancel"
          className="rounded-md p-1 text-muted-foreground hover:text-foreground"
        >
          <X className="h-3.5 w-3.5" />
        </button>
      </div>
      <p className="text-xs text-muted-foreground">{HINTS[provider]}</p>
      <div className="space-y-1.5">
        <Label htmlFor="integration-label">Label</Label>
        <Input
          id="integration-label"
          value={label}
          onChange={(e) => setLabel(e.target.value)}
          placeholder="e.g. #alerts"
          maxLength={80}
          required
        />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="integration-url">Webhook URL</Label>
        <Input
          id="integration-url"
          type="url"
          value={webhookUrl}
          onChange={(e) => setWebhookUrl(e.target.value)}
          placeholder={
            provider === "slack"
              ? "https://hooks.slack.com/services/..."
              : "https://discord.com/api/webhooks/..."
          }
          required
          className="font-mono text-xs"
        />
      </div>
      <div className="flex items-center justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onCancel} disabled={busy}>
          Cancel
        </Button>
        <Button type="submit" disabled={busy}>
          {busy && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
          Connect
        </Button>
      </div>
    </form>
  );
}
