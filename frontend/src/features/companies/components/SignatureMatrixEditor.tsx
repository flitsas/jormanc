import { useEffect, useState } from "react";
import { useSignatureMatrix, useUpdateSignatureMatrix } from "../api/companies.api.js";

const ACTOR_ROLES = ["vendedor", "comprador", "representante_legal"] as const;
const SIGNATURE_TYPES = ["identidad_digital", "firma_pantalla", "preasignada"] as const;

interface SignatureMatrixEditorProps {
  companyId: string;
  onSaved: (msg: string) => void;
}

export function SignatureMatrixEditor({ companyId, onSaved }: SignatureMatrixEditorProps) {
  const { data, isLoading, error } = useSignatureMatrix(companyId);
  const update = useUpdateSignatureMatrix(companyId);
  const [rows, setRows] = useState<Record<string, string>>({});

  useEffect(() => {
    if (data) {
      const map: Record<string, string> = {};
      for (const entry of data) {
        map[entry.actorRole] = entry.signatureType;
      }
      for (const role of ACTOR_ROLES) {
        if (!map[role]) map[role] = "identidad_digital";
      }
      setRows(map);
    }
  }, [data]);

  if (isLoading) {
    return <p className="text-sm text-flit-muted" aria-busy="true">Cargando matriz de firmas…</p>;
  }
  if (error) {
    return <p className="text-sm text-red-600" role="alert">{error.message}</p>;
  }

  async function handleSave() {
    const entries = ACTOR_ROLES.map((role) => ({
      actorRole: role,
      signatureType: rows[role] ?? "identidad_digital",
    }));
    await update.mutateAsync(entries);
    onSaved("Matriz de firmas actualizada");
  }

  return (
    <div className="space-y-4" role="tabpanel">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b text-left text-xs font-semibold uppercase text-flit-muted">
            <th className="py-2 pr-4">Actor</th>
            <th className="py-2">Tipo de firma</th>
          </tr>
        </thead>
        <tbody>
          {ACTOR_ROLES.map((role) => (
            <tr key={role} className="border-b border-slate-100 dark:border-flit-border-dark/50">
              <td className="py-3 pr-4 font-medium capitalize">{role.replace(/_/g, " ")}</td>
              <td className="py-3">
                <select
                  className="w-full rounded-lg border border-slate-300 px-3 py-1.5 text-sm dark:border-flit-border-dark dark:bg-slate-800"
                  value={rows[role] ?? "identidad_digital"}
                  onChange={(e) => setRows((r) => ({ ...r, [role]: e.target.value }))}
                  aria-label={`Tipo de firma para ${role}`}
                >
                  {SIGNATURE_TYPES.map((t) => (
                    <option key={t} value={t}>
                      {t.replace(/_/g, " ")}
                    </option>
                  ))}
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <button
        type="button"
        onClick={() => void handleSave()}
        disabled={update.isPending}
        className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
      >
        Guardar matriz
      </button>
    </div>
  );
}
