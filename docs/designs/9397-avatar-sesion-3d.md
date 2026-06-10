# Diseño técnico — Feature #9397 · Avatar sesión 3D

**Estado:** Aprobado (implementación directa, alcance UI mínimo)  
**HU:** #9398 `[FRONTEND] – UI – Avatar sesión 3D en header`

## Decisión

Reemplazar el SVG lineal en `DashboardLayout` por el componente `UserSessionAvatar` en `shared/components/ui/`, usando **SVG con gradientes + filtros** y **contenedor con relieve CSS** (sin librerías 3D).

## Archivos

| Acción | Ruta |
|--------|------|
| Crear | `frontend/src/shared/components/ui/UserSessionAvatar.tsx` |
| Crear | `frontend/src/shared/components/ui/UserSessionAvatar.spec.tsx` |
| Modificar | `frontend/src/shared/components/ui/DashboardLayout.tsx` |

## Detalle visual

- Contenedor `h-9 w-9` (alineado con `ThemeToggle` / `HelpButton`).
- Gradiente vertical azul + sombra inferior (“botón físico”).
- Icono usuario relleno con `linearGradient` en cabeza/cuerpo y highlight en cabeza.
- `useId()` para IDs únicos de gradientes en SVG.
- `aria-hidden="true"` (decorativo; login deshabilitado).

## Accesibilidad

- No captura foco; no interfiere con tab order de controles adyacentes.
- Contraste del icono sobre fondo del botón validado en claro/oscuro.

## QA

- Vitest: render sin error, presencia de SVG con gradientes.
- Manual: verificar header en tema claro y oscuro.
