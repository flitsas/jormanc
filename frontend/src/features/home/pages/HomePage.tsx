/**
 * Pantalla principal (shell). Punto de partida para Trámites 2.0.
 */
export function HomePage() {
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center p-6 sm:p-8 text-center">
      <h1 className="text-2xl font-bold text-flit-heading dark:text-flit-heading-dark">FLIT</h1>
      <p className="mt-2 max-w-md text-sm text-flit-muted dark:text-flit-muted-dark">
        Plataforma lista para desarrollar las nuevas funcionalidades de trámites.
      </p>
    </div>
  );
}
