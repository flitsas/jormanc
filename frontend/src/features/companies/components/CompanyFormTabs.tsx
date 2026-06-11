import { useState } from "react";
import { useUpdateCompanyConfig } from "../api/companies.api.js";
import { SignatureMatrixEditor } from "./SignatureMatrixEditor.js";
import { UserExceptionsManager } from "./UserExceptionsManager.js";
import { IntegrationLogsTable } from "./IntegrationLogsTable.js";
import type { CompanyListItem } from "../api/companies.schemas.js";

type TabId = "empresa" | "notificaciones" | "firmas" | "excepciones" | "logs";

const TABS: { id: TabId; label: string; icon: string }[] = [
  { id: "empresa", label: "Configuración Empresa", icon: "pi-building" },
  { id: "notificaciones", label: "Notificaciones", icon: "pi-envelope" },
  { id: "firmas", label: "Matriz de Firmas", icon: "pi-pencil" },
  { id: "excepciones", label: "Excepciones", icon: "pi-users" },
  { id: "logs", label: "Logs RUNT", icon: "pi-list" },
];

interface CompanyFormTabsProps {
  company: CompanyListItem;
  onClose: () => void;
}

export function CompanyFormTabs({ company, onClose }: CompanyFormTabsProps) {
  const [activeTab, setActiveTab] = useState<TabId>("empresa");
  const [toast, setToast] = useState<string | null>(null);
  const updateConfig = useUpdateCompanyConfig(company.id);

  const [empresaForm, setEmpresaForm] = useState({
    only_own_vehicles: true,
    baul_firmas_enabled: false,
  });
  const [notifForm, setNotifForm] = useState({
    notification_target: "admin",
    smtp_mode: "api_cliente",
  });

  function showToast(msg: string) {
    setToast(msg);
    setTimeout(() => setToast(null), 3000);
  }

  async function saveEmpresa() {
    await updateConfig.mutateAsync({
      onlyOwnVehicles: empresaForm.only_own_vehicles,
      baulFirmasEnabled: empresaForm.baul_firmas_enabled,
      notificationTarget: "admin",
      smtpMode: "api_cliente",
    });
    showToast("Configuración de empresa guardada");
  }

  async function saveNotificaciones() {
    await updateConfig.mutateAsync({
      onlyOwnVehicles: true,
      baulFirmasEnabled: false,
      notificationTarget: notifForm.notification_target,
      smtpMode: notifForm.smtp_mode,
    });
    showToast("Configuración de notificaciones guardada");
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="company-form-title"
    >
      <div className="flex max-h-[90vh] w-full max-w-3xl flex-col rounded-xl bg-white shadow-xl dark:bg-flit-surface-dark">
        <header className="flex items-center justify-between border-b border-slate-200 px-6 py-4 dark:border-flit-border-dark">
          <div>
            <h2
              id="company-form-title"
              className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
            >
              {company.name}
            </h2>
            <p className="text-sm text-flit-muted">
              NIT {company.nit} · {company.tenantSlug}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 hover:bg-slate-100 dark:hover:bg-slate-700"
            aria-label="Cerrar"
          >
            <i className="pi pi-times" />
          </button>
        </header>

        <nav
          className="flex gap-1 overflow-x-auto border-b border-slate-200 px-4 dark:border-flit-border-dark"
          role="tablist"
        >
          {TABS.map((tab) => (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={activeTab === tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-1.5 whitespace-nowrap border-b-2 px-3 py-2.5 text-sm font-medium transition-colors ${
                activeTab === tab.id
                  ? "border-flit-primary text-flit-primary"
                  : "border-transparent text-flit-muted hover:text-flit-heading"
              }`}
            >
              <i className={`pi ${tab.icon} text-xs`} aria-hidden="true" />
              {tab.label}
            </button>
          ))}
        </nav>

        <div className="flex-1 overflow-y-auto p-6">
          {activeTab === "empresa" && (
            <div className="space-y-4" role="tabpanel">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={empresaForm.only_own_vehicles}
                  onChange={(e) =>
                    setEmpresaForm((f) => ({ ...f, only_own_vehicles: e.target.checked }))
                  }
                />
                Solo vehículos propios
              </label>
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={empresaForm.baul_firmas_enabled}
                  onChange={(e) =>
                    setEmpresaForm((f) => ({ ...f, baul_firmas_enabled: e.target.checked }))
                  }
                />
                Baúl de firmas habilitado
              </label>
              <button
                type="button"
                onClick={() => void saveEmpresa()}
                disabled={updateConfig.isPending}
                className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
              >
                Guardar
              </button>
            </div>
          )}

          {activeTab === "notificaciones" && (
            <div className="space-y-4" role="tabpanel">
              <div>
                <label className="block text-sm font-medium mb-1">Destino notificaciones</label>
                <select
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
                  value={notifForm.notification_target}
                  onChange={(e) =>
                    setNotifForm((f) => ({ ...f, notification_target: e.target.value }))
                  }
                >
                  <option value="admin">Administrador</option>
                  <option value="all_users">Todos los usuarios</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Modo SMTP</label>
                <select
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
                  value={notifForm.smtp_mode}
                  onChange={(e) => setNotifForm((f) => ({ ...f, smtp_mode: e.target.value }))}
                >
                  <option value="api_cliente">API Cliente</option>
                  <option value="smtp_flit">SMTP FLIT</option>
                </select>
              </div>
              <button
                type="button"
                onClick={() => void saveNotificaciones()}
                disabled={updateConfig.isPending}
                className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
              >
                Guardar
              </button>
            </div>
          )}

          {activeTab === "firmas" && (
            <SignatureMatrixEditor companyId={company.id} onSaved={showToast} />
          )}
          {activeTab === "excepciones" && (
            <UserExceptionsManager companyId={company.id} onSaved={showToast} />
          )}
          {activeTab === "logs" && <IntegrationLogsTable tenantId={company.tenantId} />}
        </div>

        {toast && (
          <div
            className="mx-6 mb-4 rounded-lg bg-emerald-50 px-4 py-2 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300"
            role="status"
          >
            {toast}
          </div>
        )}
      </div>
    </div>
  );
}
