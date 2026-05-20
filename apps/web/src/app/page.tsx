import { ArrowRight, GitBranch, Github, Info, Workflow, Zap } from "lucide-react";
import Link from "next/link";

export default function LandingPage() {
  return (
    <main className="relative min-h-screen overflow-hidden">
      <div className="pointer-events-none absolute inset-0 -z-10 [background:radial-gradient(60%_60%_at_50%_0%,hsl(var(--primary)/0.08),transparent)]" />

      <nav className="container flex h-16 items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-md border bg-card">
            <Workflow className="h-4 w-4 text-primary" />
          </div>
          <span className="text-sm font-semibold tracking-tight">NexusFlow</span>
        </div>
        <div className="flex items-center gap-2">
          <a
            href="https://github.com/SebastianLevano/NexusFlow"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            <Github className="h-3.5 w-3.5" />
            Source
          </a>
          <Link
            href="/login"
            className="rounded-md px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            Sign in
          </Link>
          <Link
            href="/register"
            className="rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90"
          >
            Get started
          </Link>
        </div>
      </nav>

      <section className="container flex flex-col items-center pt-24 pb-32 text-center">
        <div className="mb-6 inline-flex items-center gap-2 rounded-full border bg-card px-3 py-1 text-xs text-muted-foreground">
          <span className="h-1.5 w-1.5 rounded-full bg-primary" />
          Now in early access
        </div>
        <h1 className="max-w-3xl text-balance text-5xl font-semibold tracking-tight md:text-6xl">
          Workflow automation,{" "}
          <span className="bg-gradient-to-b from-foreground to-muted-foreground bg-clip-text text-transparent">
            reimagined.
          </span>
        </h1>
        <p className="mt-6 max-w-xl text-balance text-muted-foreground md:text-lg">
          Connect triggers and actions across your stack. Build reliable automation pipelines in minutes,
          not days.
        </p>
        <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
          <Link
            href="/register"
            className="inline-flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90"
          >
            Start building <ArrowRight className="h-4 w-4" />
          </Link>
          <Link
            href="#features"
            className="inline-flex items-center rounded-md border bg-card px-4 py-2 text-sm font-medium transition-colors hover:bg-accent"
          >
            See how it works
          </Link>
        </div>

        <div className="mt-8 inline-flex max-w-md items-start gap-2 rounded-md border border-amber-500/20 bg-amber-500/5 px-3 py-2 text-left text-xs text-amber-200/90">
          <Info className="mt-0.5 h-3.5 w-3.5 shrink-0 text-amber-400" />
          <span>
            <strong className="font-medium text-amber-100">Portfolio demo —</strong> the backend
            runs on a free tier and sleeps after 15 min idle. Your first request may take{" "}
            ~30 s while it wakes up, then it&apos;s instant.
          </span>
        </div>
      </section>

      <section id="features" className="container grid gap-4 pb-32 md:grid-cols-3">
        <Feature
          icon={<Zap className="h-4 w-4" />}
          title="Trigger anything"
          description="Webhooks, schedules, and event-based triggers from any service you already use."
        />
        <Feature
          icon={<GitBranch className="h-4 w-4" />}
          title="Composable actions"
          description="Chain HTTP requests, database writes, notifications and more with typed variable passing."
        />
        <Feature
          icon={<Workflow className="h-4 w-4" />}
          title="Live observability"
          description="Watch executions stream in real-time. Inspect every step, every payload."
        />
      </section>
    </main>
  );
}

function Feature({
  icon,
  title,
  description,
}: {
  icon: React.ReactNode;
  title: string;
  description: string;
}) {
  return (
    <div className="group rounded-lg border bg-card p-6 transition-colors hover:border-border/80">
      <div className="mb-4 flex h-8 w-8 items-center justify-center rounded-md border bg-background text-primary">
        {icon}
      </div>
      <h3 className="mb-1.5 text-sm font-medium">{title}</h3>
      <p className="text-sm text-muted-foreground">{description}</p>
    </div>
  );
}
