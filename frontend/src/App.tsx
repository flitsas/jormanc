import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { BrowserRouter, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { DashboardLayout } from "./shared/components/ui/DashboardLayout.js";
import { ThemeProvider } from "./shared/hooks/use-theme.js";
import { useSignalR } from "./shared/hooks/useSignalR.js";
import { HomePage } from "./features/home/pages/HomePage.js";
import { LoginPage } from "./features/auth/pages/LoginPage.js";
import { InvitePage } from "./features/auth/pages/InvitePage.js";
import { ForgotPasswordPage } from "./features/auth/pages/ForgotPasswordPage.js";
import { ResetPasswordPage } from "./features/auth/pages/ResetPasswordPage.js";
import { UsersPage } from "./features/users-roles/pages/UsersPage.js";
import { CompaniesPage } from "./features/companies/pages/CompaniesPage.js";
import { ProcedureTypeEditorPage } from "./features/procedures-config/pages/ProcedureTypeEditorPage.js";
import { ProceduresPage } from "./features/procedures/pages/ProceduresPage.js";
import { ProcedureDetailPage } from "./features/procedures/pages/ProcedureDetailPage.js";
import { DocumentAdminPage } from "./features/documents/pages/DocumentAdminPage.js";
import { DashboardPage } from "./features/dashboard/pages/DashboardPage.js";
import { OtAdminPage } from "./features/ot-admin/pages/OtAdminPage.js";
import { getStoredToken } from "./features/auth/api/auth.api.js";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
    mutations: { retry: 0 },
  },
});

function RequireAuth({ children }: { children: ReactNode }) {
  const location = useLocation();
  const isAuthenticated = !!getStoredToken();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}

const NAV_ITEMS = [
  {
    id: "dashboard",
    label: "Dashboard",
    href: "/dashboard",
    icon: <i className="pi pi-chart-pie text-base" aria-hidden="true" />,
  },
  {
    id: "companies",
    label: "Compañías",
    href: "/admin/companies",
    icon: <i className="pi pi-building text-base" aria-hidden="true" />,
  },
  {
    id: "ot-admin",
    label: "OT",
    href: "/admin/ot",
    icon: <i className="pi pi-map-marker text-base" aria-hidden="true" />,
  },
  {
    id: "users",
    label: "Usuarios",
    href: "/admin/users",
    icon: <i className="pi pi-users text-base" aria-hidden="true" />,
  },
  {
    id: "procedure-types",
    label: "Parametrizador",
    href: "/admin/procedure-types",
    icon: <i className="pi pi-sitemap text-base" aria-hidden="true" />,
  },
  {
    id: "procedures",
    label: "Trámites",
    href: "/procedures",
    icon: <i className="pi pi-folder-open text-base" aria-hidden="true" />,
  },
  {
    id: "documents",
    label: "Documentos",
    href: "/admin/documents",
    icon: <i className="pi pi-file text-base" aria-hidden="true" />,
  },
];

function AppShell({ children }: { children: ReactNode }) {
  useSignalR();

  return (
    <DashboardLayout navItems={NAV_ITEMS} title="FLIT" subtitle="Trámites" rootHref="/">
      {children}
    </DashboardLayout>
  );
}

function AppRoutes() {
  return (
    <Routes>
      {/* Public auth routes */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/invite/:token" element={<InvitePage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password/:token" element={<ResetPasswordPage />} />

      {/* Protected routes */}
      <Route
        path="/"
        element={
          <RequireAuth>
            <AppShell>
              <HomePage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/dashboard"
        element={
          <RequireAuth>
            <AppShell>
              <DashboardPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/admin/users"
        element={
          <RequireAuth>
            <AppShell>
              <UsersPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/admin/companies"
        element={
          <RequireAuth>
            <AppShell>
              <CompaniesPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/admin/ot"
        element={
          <RequireAuth>
            <AppShell>
              <OtAdminPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/admin/procedure-types/:id"
        element={
          <RequireAuth>
            <AppShell>
              <ProcedureTypeEditorPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/admin/documents"
        element={
          <RequireAuth>
            <AppShell>
              <DocumentAdminPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/admin/document-types/:documentTypeId/templates"
        element={
          <RequireAuth>
            <AppShell>
              <DocumentAdminPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/procedures"
        element={
          <RequireAuth>
            <AppShell>
              <ProceduresPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/procedures/:id"
        element={
          <RequireAuth>
            <AppShell>
              <ProcedureDetailPage />
            </AppShell>
          </RequireAuth>
        }
      />

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </ThemeProvider>
      {import.meta.env.DEV && <ReactQueryDevtools />}
    </QueryClientProvider>
  );
}
