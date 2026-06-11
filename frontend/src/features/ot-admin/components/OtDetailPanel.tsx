import { useState } from "react";
import { useUpdateOtOrganism } from "../api/ot-admin.api.js";
import type { OtOrganism } from "../api/ot-admin.schemas.js";
import { DocumentOrderDragDrop } from "./DocumentOrderDragDrop.js";
import { OtIntegrationLogsTable } from "./OtIntegrationLogsTable.js";
import { OtLabelsManager } from "./OtLabelsManager.js";
import { OtModeSwitch } from "./OtModeSwitch.js";
import { QuipuxConfigForm } from "./QuipuxConfigForm.js";

type TabId = "general" | "quipux" | "logs" | "prelacion";

const TABS: { id: TabId; label: string; icon: string }[] = [
  { id: "general", label: "General", icon: "pi-cog" },
  { id: "quipux", label: "Quipux", icon: "pi-link" },
  { id: "prelacion", label: "Prelación", icon: "pi-sort-alt" },
  { id: "logs", label: "Logs Quipux", icon: "pi-list" },
];

interface OtDetailPanelProps {
  organism: OtOrganism;
  onClose: () => void;
  onUpdated: (organism: OtOrganism) => void;
}

export function OtDetailPanel({ organism, onClose, onUpdated }: OtDetailPanelProps) {
  const [activeTab, setActiveTab] = useState<TabId>("general");
  const [current, setCurrent] = useState(organism);
  const [name, setName] = useState(organism.name);
  const [saveError, setSaveError] = useState<string | null>(null);
  const updateOt = useUpdateOtOrganism(organism.id);

  function handleOrganismUpdate(next: OtOrganism) {
    setCurrent(next);
    setName(next.name);
    onUpdated(next);
  }

  async function handleSaveName() {
    setSaveError(null);
    try {
      const updated = await updateOt.mutateAsync({ name: name.trim() });
      handleOrganismUpdate(updated);
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : "Error al guardar");
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="ot-detail-title"
    >
      <div className="flex max-h-[90vh] w-full max-w-3xl flex-col rounded-xl bg-white shadow-xl dark:bg-flit-surface-dark">
        <header className="flex items-center justify-between border-b border-slate-200 px-6 py-4 dark:border-flit-border-dark">
          <div>
            <h2
              id="ot-detail-title"
              className="text-lg font-semibold text-flit-heading dark:text-flit-heading-dark"
            >
              {current.name}
            </h2>
            <p className="text-sm text-flit-muted">
              {current.slug} · {current.mode === "qx" ? "Modo QX" : "Modo Dashboard"}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 hover:bg-slate-100 dark:hover:bg-slate-700"
            aria-label="Cerrar"
          >
            <i className="pi pi-times" aria-hidden="true" />
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
          {activeTab === "general" && (
            <div className="space-y-6" role="tabpanel">
              <div>
                <label htmlFor="ot-edit-name" className="mb-1 block text-sm font-medium">
                  Nombre del OT
                </label>
                <div className="flex flex-col gap-2 sm:flex-row">
                  <input
                    id="ot-edit-name"
                    className="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                  />
                  <button
                    type="button"
                    onClick={() => void handleSaveName()}
                    disabled={updateOt.isPending || name.trim() === current.name}
                    className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                  >
                    Guardar nombre
                  </button>
                </div>
                {saveError && (
                  <p className="mt-2 text-sm text-red-600" role="alert">
                    {saveError}
                  </p>
                )}
              </div>

              <OtModeSwitch organism={current} onModeChanged={handleOrganismUpdate} />
            </div>
          )}

          {activeTab === "quipux" && (
            <div role="tabpanel">
              <QuipuxConfigForm organism={current} onSaved={handleOrganismUpdate} />
            </div>
          )}

          {activeTab === "prelacion" && (
            <div role="tabpanel" className="space-y-8">
              <section aria-labelledby="ot-prelacion-heading">
                <h3
                  id="ot-prelacion-heading"
                  className="mb-3 text-sm font-semibold uppercase tracking-wide text-flit-muted"
                >
                  Prelación documental
                </h3>
                <DocumentOrderDragDrop otId={current.id} />
              </section>
              <section aria-labelledby="ot-labels-heading">
                <h3
                  id="ot-labels-heading"
                  className="mb-3 text-sm font-semibold uppercase tracking-wide text-flit-muted"
                >
                  Etiquetas personalizadas
                </h3>
                <OtLabelsManager otId={current.id} />
              </section>
            </div>
          )}

          {activeTab === "logs" && (
            <div role="tabpanel">
              <OtIntegrationLogsTable otId={current.id} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
