interface EmptyUserCardProps {
  userName?: string;
}

export function EmptyUserCard({ userName }: EmptyUserCardProps) {
  return (
    <div
      className="rounded-lg border border-dashed border-flit-border bg-flit-canvas/50 px-4 py-6 text-center dark:border-flit-border-dark dark:bg-slate-900/40"
      role="status"
      aria-label={
        userName
          ? `${userName} no ha radicado trámites en el período`
          : "Usuario sin trámites en el período"
      }
    >
      <i className="pi pi-user-minus mb-2 text-2xl text-flit-muted" aria-hidden="true" />
      <p className="text-sm font-medium text-flit-heading dark:text-flit-heading-dark">
        Este usuario no ha radicado ningún trámite
      </p>
      {userName ? <p className="mt-1 text-xs text-flit-muted">{userName}</p> : null}
    </div>
  );
}
