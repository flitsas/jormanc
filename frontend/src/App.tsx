import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { DashboardLayout } from "./shared/components/ui/DashboardLayout.js";
import { HomeStyleProvider } from "./shared/hooks/use-home-style.js";
import { ThemeProvider } from "./shared/hooks/use-theme.js";
import { HomePage } from "./features/home/pages/HomePage.js";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
    mutations: { retry: 0 },
  },
});

function AppShell({ children }: { children: ReactNode }) {
  return (
    <DashboardLayout navItems={[]} title="FLIT" subtitle="Trámites" rootHref="/">
      {children}
    </DashboardLayout>
  );
}

function AppRoutes() {
  return (
    <Routes>
      <Route
        path="/"
        element={
          <AppShell>
            <HomePage />
          </AppShell>
        }
      />
    </Routes>
  );
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <HomeStyleProvider>
          <BrowserRouter>
            <AppRoutes />
          </BrowserRouter>
        </HomeStyleProvider>
      </ThemeProvider>
      {import.meta.env.DEV && <ReactQueryDevtools />}
    </QueryClientProvider>
  );
}
