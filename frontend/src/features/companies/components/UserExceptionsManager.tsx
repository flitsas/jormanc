import { useState } from "react";
import {
  useUserExceptions,
  useAddUserException,
  useRemoveUserException,
} from "../api/companies.api.js";

interface UserExceptionsManagerProps {
  companyId: string;
  onSaved: (msg: string) => void;
}

export function UserExceptionsManager({ companyId, onSaved }: UserExceptionsManagerProps) {
  const { data, isLoading, error } = useUserExceptions(companyId);
  const addMutation = useAddUserException(companyId);
  const removeMutation = useRemoveUserException(companyId);
  const [userIdInput, setUserIdInput] = useState("");

  async function handleAdd() {
    if (!userIdInput.trim()) return;
    await addMutation.mutateAsync(userIdInput.trim());
    setUserIdInput("");
    onSaved("Usuario agregado a lista blanca");
  }

  async function handleRemove(userId: string) {
    await removeMutation.mutateAsync(userId);
    onSaved("Usuario eliminado de lista blanca");
  }

  if (isLoading) {
    return <p className="text-sm text-flit-muted" aria-busy="true">Cargando excepciones…</p>;
  }
  if (error) {
    return <p className="text-sm text-red-600" role="alert">{error.message}</p>;
  }

  const exceptions = data ?? [];

  return (
    <div className="space-y-4" role="tabpanel">
      <div className="flex gap-2">
        <input
          type="text"
          placeholder="UUID del usuario"
          value={userIdInput}
          onChange={(e) => setUserIdInput(e.target.value)}
          className="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm dark:border-flit-border-dark dark:bg-slate-800"
          aria-label="ID de usuario para lista blanca"
        />
        <button
          type="button"
          onClick={() => void handleAdd()}
          disabled={addMutation.isPending}
          className="rounded-lg bg-flit-primary px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
        >
          Agregar
        </button>
      </div>

      {exceptions.length === 0 ? (
        <p className="text-sm text-flit-muted" role="status">No hay usuarios en la lista blanca.</p>
      ) : (
        <ul className="divide-y divide-slate-100 dark:divide-flit-border-dark/50">
          {exceptions.map((ex) => (
            <li key={ex.id} className="flex items-center justify-between py-2">
              <span className="font-mono text-xs text-flit-heading dark:text-flit-heading-dark">
                {ex.userId}
              </span>
              <button
                type="button"
                onClick={() => void handleRemove(ex.userId)}
                disabled={removeMutation.isPending}
                className="text-sm text-red-600 hover:underline disabled:opacity-50"
              >
                Eliminar
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
