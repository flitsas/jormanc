import { Fragment, useState } from "react";
import type { ApiConnector } from "../api/procedures-config.schemas.js";

interface BindingRow {
  key: string;
  value: string;
}

interface ApiConnectorsPanelProps {
  connectors: ApiConnector[];
  onSaveBindings: (connectorId: string, bindings: Record<string, string>) => Promise<void>;
  isSaving?: boolean;
}

function bindingsToRows(bindings: Record<string, string>): BindingRow[] {
  const rows = Object.entries(bindings).map(([key, value]) => ({ key, value }));
  return rows.length > 0 ? rows : [{ key: "", value: "" }];
}

function rowsToBindings(rows: BindingRow[]): Record<string, string> {
  return rows.reduce<Record<string, string>>((acc, row) => {
    if (row.key.trim()) acc[row.key.trim()] = row.value.trim();
    return acc;
  }, {});
}

export function ApiConnectorsPanel({
  connectors,
  onSaveBindings,
  isSaving = false,
}: ApiConnectorsPanelProps) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [rows, setRows] = useState<BindingRow[]>([]);

  function startEdit(connector: ApiConnector) {
    setEditingId(connector.id);
    setRows(bindingsToRows(connector.paramBindings));
  }

  async function handleSave(connectorId: string) {
    await onSaveBindings(connectorId, rowsToBindings(rows));
    setEditingId(null);
  }

  if (connectors.length === 0) {
    return (
      <p className="text-sm text-flit-muted dark:text-flit-muted-dark" role="status">
        No hay conectores API configurados para este tipo de trámite.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm" aria-label="Conectores API">
        <thead>
          <tr className="border-b border-flit-border text-left dark:border-flit-border-dark">
            <th className="p-2 font-semibold">Nombre</th>
            <th className="p-2 font-semibold">Endpoint</th>
            <th className="p-2 font-semibold">Verbo</th>
            <th className="p-2 font-semibold">Paso</th>
            <th className="p-2 font-semibold">Acciones</th>
          </tr>
        </thead>
        <tbody>
          {connectors.map((conn) => (
            <Fragment key={conn.id}>
              <tr className="border-b border-flit-border dark:border-flit-border-dark">
                <td className="p-2">{conn.name}</td>
                <td className="p-2 font-mono text-xs">{conn.endpoint}</td>
                <td className="p-2">{conn.httpVerb}</td>
                <td className="p-2">{conn.stepOrder}</td>
                <td className="p-2">
                  <button
                    type="button"
                    onClick={() => startEdit(conn)}
                    className="text-flit-primary hover:underline"
                  >
                    Editar bindings
                  </button>
                </td>
              </tr>
              {editingId === conn.id && (
                <tr>
                  <td colSpan={5} className="bg-flit-surface p-4 dark:bg-flit-surface-dark">
                    <p className="mb-2 text-sm font-medium">param_bindings</p>
                    <div className="space-y-2">
                      {rows.map((row, index) => (
                        <div key={index} className="flex gap-2">
                          <input
                            type="text"
                            placeholder="parámetro"
                            aria-label={`Binding clave ${index + 1}`}
                            value={row.key}
                            onChange={(e) =>
                              setRows((prev) =>
                                prev.map((r, i) =>
                                  i === index ? { ...r, key: e.target.value } : r,
                                ),
                              )
                            }
                            className="flex-1 rounded border border-flit-border px-2 py-1 dark:border-flit-border-dark dark:bg-flit-surface-dark"
                          />
                          <input
                            type="text"
                            placeholder="step_1.field_placa"
                            aria-label={`Binding valor ${index + 1}`}
                            value={row.value}
                            onChange={(e) =>
                              setRows((prev) =>
                                prev.map((r, i) =>
                                  i === index ? { ...r, value: e.target.value } : r,
                                ),
                              )
                            }
                            className="flex-1 rounded border border-flit-border px-2 py-1 dark:border-flit-border-dark dark:bg-flit-surface-dark"
                          />
                        </div>
                      ))}
                      <button
                        type="button"
                        onClick={() => setRows((prev) => [...prev, { key: "", value: "" }])}
                        className="text-sm text-flit-primary hover:underline"
                      >
                        + Agregar binding
                      </button>
                    </div>
                    <div className="mt-3 flex gap-2">
                      <button
                        type="button"
                        disabled={isSaving}
                        onClick={() => void handleSave(conn.id)}
                        className="rounded-lg bg-flit-primary px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
                      >
                        Guardar
                      </button>
                      <button
                        type="button"
                        onClick={() => setEditingId(null)}
                        className="rounded-lg border border-flit-border px-3 py-1.5 text-sm dark:border-flit-border-dark"
                      >
                        Cancelar
                      </button>
                    </div>
                  </td>
                </tr>
              )}
            </Fragment>
          ))}
        </tbody>
      </table>
    </div>
  );
}
