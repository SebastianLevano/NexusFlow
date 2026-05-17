"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Save } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { StepEditor } from "./step-editor";
import { workflowsApi } from "@/lib/workflows/api";
import {
  TRIGGER_OPTIONS,
  workflowFormSchema,
  type WorkflowFormInput,
  type WorkflowFormOutput,
} from "@/lib/workflows/schemas";
import type { Workflow } from "@/lib/workflows/types";

interface Props {
  workflow?: Workflow;
}

function buildDefaults(workflow?: Workflow): WorkflowFormInput {
  if (!workflow) {
    return {
      name: "",
      description: "",
      triggerType: "webhook",
      triggerConfigRaw: TRIGGER_OPTIONS[0].sample,
      steps: [],
    };
  }
  return {
    name: workflow.name,
    description: workflow.description ?? "",
    triggerType: workflow.triggerType,
    triggerConfigRaw: JSON.stringify(workflow.triggerConfig, null, 2),
    steps: workflow.steps
      .sort((a, b) => a.orderIndex - b.orderIndex)
      .map((s) => ({ actionType: s.actionType, configRaw: JSON.stringify(s.config, null, 2) })),
  };
}

export function WorkflowForm({ workflow }: Props) {
  const router = useRouter();
  const isEdit = !!workflow;

  const form = useForm<WorkflowFormInput, unknown, WorkflowFormOutput>({
    resolver: zodResolver(workflowFormSchema),
    defaultValues: buildDefaults(workflow),
  });

  useEffect(() => {
    form.reset(buildDefaults(workflow));
  }, [workflow, form]);

  async function onSubmit(values: WorkflowFormOutput) {
    const payload = {
      name: values.name,
      description: values.description?.trim() ? values.description : null,
      triggerType: values.triggerType,
      triggerConfig: values.triggerConfigRaw,
      steps: values.steps.map((s, i) => ({
        orderIndex: i,
        actionType: s.actionType,
        config: s.configRaw,
      })),
    };

    try {
      const saved = isEdit
        ? await workflowsApi.update(workflow!.id, payload)
        : await workflowsApi.create(payload);
      toast.success(isEdit ? "Workflow updated." : "Workflow created.");
      router.replace(`/workflows/${saved.id}`);
      router.refresh();
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      toast.error(message ?? "Could not save workflow.");
    }
  }

  const submitting = form.formState.isSubmitting;
  const triggerType = form.watch("triggerType");

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">
      <section className="rounded-lg border bg-card p-6">
        <h2 className="mb-4 text-sm font-medium">Details</h2>
        <div className="grid gap-4">
          <div className="space-y-1.5">
            <Label htmlFor="name">Name</Label>
            <Input id="name" aria-invalid={!!form.formState.errors.name} {...form.register("name")} />
            {form.formState.errors.name && (
              <p className="text-xs text-destructive">{form.formState.errors.name.message}</p>
            )}
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="description">Description</Label>
            <Input id="description" {...form.register("description")} />
          </div>
        </div>
      </section>

      <section className="rounded-lg border bg-card p-6">
        <h2 className="mb-4 text-sm font-medium">Trigger</h2>
        <div className="grid gap-4">
          <div className="space-y-1.5">
            <Label htmlFor="triggerType">Type</Label>
            <Controller
              control={form.control}
              name="triggerType"
              render={({ field }) => (
                <Select
                  id="triggerType"
                  {...field}
                  onChange={(e) => {
                    field.onChange(e);
                    const sample = TRIGGER_OPTIONS.find((o) => o.value === e.target.value)?.sample;
                    if (sample) form.setValue("triggerConfigRaw", sample);
                  }}
                >
                  {TRIGGER_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </Select>
              )}
            />
            <p className="text-xs text-muted-foreground">
              {triggerType === "webhook"
                ? "POST to /hooks/{workflowId}/{secret} to trigger this workflow."
                : "Provide a cron expression in the config below."}
            </p>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="triggerConfigRaw">Config (JSON)</Label>
            <Textarea
              id="triggerConfigRaw"
              rows={5}
              aria-invalid={!!form.formState.errors.triggerConfigRaw}
              {...form.register("triggerConfigRaw")}
            />
            {form.formState.errors.triggerConfigRaw && (
              <p className="text-xs text-destructive">{form.formState.errors.triggerConfigRaw.message}</p>
            )}
          </div>
        </div>
      </section>

      <section>
        <StepEditor control={form.control} errors={form.formState.errors} />
      </section>

      <div className="flex items-center justify-end gap-2">
        <Button asChild variant="ghost" type="button">
          <Link href="/workflows">Cancel</Link>
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Save className="h-3.5 w-3.5" />}
          {isEdit ? "Save changes" : "Create workflow"}
        </Button>
      </div>
    </form>
  );
}

function extractErrorMessage(err: unknown): string | null {
  if (typeof err === "object" && err !== null && "response" in err) {
    const data = (err as { response?: { data?: { detail?: string; title?: string; errors?: Record<string, string[]> } } })
      .response?.data;
    if (data?.errors) {
      const first = Object.values(data.errors).flat()[0];
      if (first) return first;
    }
    return data?.detail ?? data?.title ?? null;
  }
  return null;
}
