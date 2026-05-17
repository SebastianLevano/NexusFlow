"use client";

import Link from "next/link";
import { ArrowDown, ArrowUp, Plug, Plus, Trash2 } from "lucide-react";
import { Controller, useFieldArray, useWatch, type Control, type FieldErrors } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { ACTION_OPTIONS, type WorkflowFormInput } from "@/lib/workflows/schemas";

interface Props {
  control: Control<WorkflowFormInput>;
  errors: FieldErrors<WorkflowFormInput>;
}

export function StepEditor({ control, errors }: Props) {
  const { fields, append, remove, move } = useFieldArray({ control, name: "steps" });

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-sm font-medium">Steps</h2>
          <p className="text-xs text-muted-foreground">Actions are executed in order, top to bottom.</p>
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() =>
            append({
              actionType: "http_request",
              configRaw: ACTION_OPTIONS.find((o) => o.value === "http_request")!.sample,
            })
          }
        >
          <Plus className="h-3.5 w-3.5" />
          Add step
        </Button>
      </div>

      {fields.length === 0 && (
        <p className="rounded-md border border-dashed bg-card/30 px-4 py-6 text-center text-sm text-muted-foreground">
          No steps yet. Add one to start building your automation.
        </p>
      )}

      <ol className="space-y-3">
        {fields.map((field, index) => {
          const stepErrors = errors.steps?.[index];
          return (
            <li key={field.id} className="rounded-lg border bg-card p-4">
              <div className="mb-3 flex items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <span className="flex h-6 w-6 items-center justify-center rounded-full border bg-background text-xs font-medium text-muted-foreground">
                    {index + 1}
                  </span>
                  <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Step</span>
                </div>
                <div className="flex items-center gap-1">
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => move(index, index - 1)}
                    disabled={index === 0}
                    aria-label="Move up"
                  >
                    <ArrowUp className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => move(index, index + 1)}
                    disabled={index === fields.length - 1}
                    aria-label="Move down"
                  >
                    <ArrowDown className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => remove(index)}
                    aria-label="Remove step"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>

              <div className="grid gap-3">
                <div className="space-y-1.5">
                  <Label htmlFor={`steps.${index}.actionType`}>Action</Label>
                  <Controller
                    control={control}
                    name={`steps.${index}.actionType`}
                    render={({ field: f }) => (
                      <Select id={`steps.${index}.actionType`} {...f}>
                        {ACTION_OPTIONS.map((o) => (
                          <option key={o.value} value={o.value}>
                            {o.label}
                          </option>
                        ))}
                      </Select>
                    )}
                  />
                  <ActionHint control={control} index={index} />
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor={`steps.${index}.configRaw`}>Config (JSON)</Label>
                  <Controller
                    control={control}
                    name={`steps.${index}.configRaw`}
                    render={({ field: f }) => (
                      <Textarea
                        id={`steps.${index}.configRaw`}
                        rows={5}
                        aria-invalid={!!stepErrors?.configRaw}
                        {...f}
                      />
                    )}
                  />
                  {stepErrors?.configRaw && (
                    <p className="text-xs text-destructive">{stepErrors.configRaw.message}</p>
                  )}
                </div>
              </div>
            </li>
          );
        })}
      </ol>
    </div>
  );
}

function ActionHint({ control, index }: { control: Control<WorkflowFormInput>; index: number }) {
  const actionType = useWatch({ control, name: `steps.${index}.actionType` });
  if (actionType !== "slack_post_message" && actionType !== "discord_post_message") return null;
  return (
    <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
      <Plug className="h-3 w-3" />
      Requires <code className="font-mono">integrationId</code> from{" "}
      <Link href="/integrations" className="text-foreground hover:underline">
        Integrations
      </Link>
      .
    </p>
  );
}
