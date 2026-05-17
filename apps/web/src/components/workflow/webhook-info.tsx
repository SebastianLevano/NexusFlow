"use client";

import { useState } from "react";
import { Check, Copy, Webhook } from "lucide-react";
import { Button } from "@/components/ui/button";

interface Props {
  workflowId: string;
  secret: string | null;
}

export function WebhookInfo({ workflowId, secret }: Props) {
  const [copied, setCopied] = useState(false);
  const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";
  const url = secret ? `${apiBase}/hooks/${workflowId}/${secret}` : null;

  async function copy() {
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      // ignore
    }
  }

  return (
    <div className="rounded-lg border bg-card p-6">
      <div className="mb-3 flex items-center gap-2">
        <Webhook className="h-4 w-4 text-muted-foreground" />
        <h2 className="text-sm font-medium">Webhook URL</h2>
      </div>
      {url ? (
        <>
          <div className="flex items-center gap-2 rounded-md border bg-background px-3 py-2 font-mono text-xs">
            <span className="min-w-0 flex-1 truncate">{url}</span>
            <Button type="button" variant="ghost" size="sm" onClick={copy} aria-label="Copy URL">
              {copied ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
            </Button>
          </div>
          <p className="mt-2 text-xs text-muted-foreground">
            POST a JSON body to this URL. The payload becomes <code className="font-mono">{"{{ trigger.* }}"}</code> in your steps.
          </p>
        </>
      ) : (
        <p className="text-xs text-muted-foreground">
          Set a <code className="font-mono">secret</code> field in the trigger config to generate a webhook URL.
        </p>
      )}
    </div>
  );
}
