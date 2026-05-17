import { AuthGuard } from "@/components/shared/auth-guard";
import { CommandPalette } from "@/components/shared/command-palette";
import { Sidebar } from "@/components/shared/sidebar";
import { PageTransition } from "@/components/shared/page-transition";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <AuthGuard>
      <div className="flex min-h-screen">
        <Sidebar />
        <main className="flex-1 overflow-auto">
          <div className="mx-auto w-full max-w-6xl px-8 py-10">
            <PageTransition>{children}</PageTransition>
          </div>
        </main>
        <CommandPalette />
      </div>
    </AuthGuard>
  );
}
