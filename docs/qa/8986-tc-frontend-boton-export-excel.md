# Test Cases — US #8986 [FRONTEND] Tramites - Boton Total descarga Excel

**Feature padre:** #8983 — [TRAMITES] Exportar total de tramites a Excel  
**HU:** #8986 — [FRONTEND] Botón Total descarga Excel con filtros actuales  
**Story Points:** 3  
**Módulo:** TRAMITES  
**Generado:** 2026-05-26  
**Dependencia:** US #8985 (endpoint backend debe estar disponible)  

---

## QA_TC07_TRAMITES_EXPORT_UI - Total se comporta como boton interactivo de exportacion

### Precondiciones
- Usuario autenticado en Flit 2.0 con acceso al módulo Trámites.
- Existen trámites visibles en el listado (`totalCount > 0`).
- La barra de filtros de `TramitesListPanel` está visible.

### Pasos
1. Navegar al módulo Trámites / Traspasos.
2. Verificar que el indicador `Total` es visible en la barra de filtros.
3. Inspeccionar el elemento HTML del indicador `Total`.
4. Verificar el rol ARIA del elemento.
5. Pasar el cursor sobre el elemento (hover).
6. Mover el foco con teclado (Tab) hasta el elemento.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| totalCount en listado | > 0 |
| Navegador | Chrome / Firefox último |

### Resultado esperado
- El indicador `Total` renderiza como `<button>` o elemento con `role="button"`.
- Tiene un `aria-label` descriptivo que indica la acción de exportar (ej: `aria-label="Exportar trámites a Excel"`).
- Al hacer hover, el cursor cambia a `pointer` y el elemento muestra estado visual diferenciado.
- Al recibir foco con teclado, muestra un outline visible (cumple WCAG 2.1 AA focus visible).

### Postcondiciones
- Ninguna.

---

## QA_TC08_TRAMITES_EXPORT_UI - Clic en Total descarga archivo xlsx

### Precondiciones
- Usuario autenticado.
- Backend de export disponible (`GET /api/v1/traspasos/tramites/export` funcional).
- Existen trámites que coinciden con los filtros actuales (`totalCount > 0`).

### Pasos
1. Navegar al módulo Trámites con filtros por defecto (sin filtros activos).
2. Hacer clic en el botón `Total`.
3. Esperar a que la descarga se inicie (máx 5 segundos).
4. Verificar que el archivo descargado tiene extensión `.xlsx`.
5. Verificar el nombre del archivo descargado.
6. Abrir el archivo y confirmar que es un Excel válido con datos.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| Filtros activos | Ninguno (listado completo) |
| Extensión esperada | `.xlsx` |
| Patrón de nombre esperado | Contiene "tramites" y fecha actual (ej: `tramites-2026-05-26.xlsx`) |

### Resultado esperado
- Se descarga automáticamente un archivo con extensión `.xlsx`.
- El nombre del archivo incluye la palabra "tramites" y la fecha actual.
- El archivo se puede abrir en Excel / LibreOffice y contiene los encabezados y datos de trámites.
- El botón vuelve a su estado normal tras completarse la descarga.

### Postcondiciones
- Eliminar el archivo descargado de la carpeta de descargas del navegador de prueba.

---

## QA_TC09_TRAMITES_EXPORT_UI - Export respeta filtros aplicados en la vista

### Precondiciones
- Usuario autenticado.
- Existen trámites con estado `Validated` y con el texto "ABC" en algún campo buscable.
- El listado con esos filtros devuelve un `totalCount` conocido (ej: 3).

### Pasos
1. Navegar al módulo Trámites.
2. Aplicar filtro `status = Validated` y `search = ABC`.
3. Verificar que el `totalCount` mostrado coincide con el total esperado (ej: 3).
4. Abrir las herramientas de red del navegador (DevTools → Network).
5. Hacer clic en el botón `Total`.
6. Inspeccionar la petición HTTP enviada al backend.
7. Verificar los query params de la petición.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| status | `Validated` |
| search | `ABC` |
| totalCount esperado | 3 (o el real en BD) |

### Resultado esperado
- La petición al API es `GET /api/v1/traspasos/tramites/export?status=Validated&search=ABC`.
- Los parámetros `status` y `search` son exactamente los mismos que los usados en el listado activo.
- El archivo Excel descargado contiene exactamente 3 filas de datos (igual al `totalCount`).
- Ningún trámite fuera de los filtros aparece en el archivo.

### Postcondiciones
- Limpiar filtros del listado.

---

## QA_TC10_TRAMITES_EXPORT_UI - Boton muestra estado de carga y se deshabilita durante peticion

### Precondiciones
- Usuario autenticado.
- El endpoint de export tiene latencia simulada > 500ms (o se puede interceptar con un mock lento).
- Existen trámites en el listado.

### Pasos
1. Navegar al módulo Trámites con datos visibles.
2. Abrir DevTools → Network → aplicar throttling lento (ej: Slow 3G).
3. Hacer clic en el botón `Total`.
4. Mientras la petición está en curso, observar el estado visual del botón.
5. Intentar hacer clic nuevamente en el botón durante la carga.
6. Esperar a que la descarga complete.
7. Verificar el estado del botón tras completarse.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| Throttling de red | Slow 3G (DevTools) |
| Doble clic durante carga | Intentado 1 vez |

### Resultado esperado
- Durante la petición, el botón muestra indicador de carga (spinner o texto "Exportando...").
- El botón tiene atributo `disabled` o `aria-busy="true"` durante la carga.
- El segundo clic durante la carga no genera una segunda petición al backend.
- Tras completarse la descarga, el botón vuelve a su estado normal habilitado.

### Postcondiciones
- Desactivar throttling de red en DevTools.

---

## QA_TC11_TRAMITES_EXPORT_UI - Boton deshabilitado cuando totalCount es cero

### Precondiciones
- Usuario autenticado.
- Se pueden aplicar filtros que resulten en `totalCount = 0` (ej: `search = ZZZNORESULTS999`).

### Pasos
1. Navegar al módulo Trámites.
2. Aplicar filtro `search = ZZZNORESULTS999` (sin resultados).
3. Verificar que el `totalCount` mostrado es 0.
4. Observar el estado del botón `Total`.
5. Intentar hacer clic en el botón.
6. Verificar que no se realiza ninguna petición al backend.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| search | `ZZZNORESULTS999` |
| totalCount esperado | 0 |

### Resultado esperado
- El botón `Total` aparece deshabilitado visualmente (`disabled` o estilos de disabled) cuando `totalCount = 0`.
- No se realiza ninguna petición al backend al intentar hacer clic.
- No se muestra ningún diálogo de descarga ni mensaje de error inesperado.
- Opcionalmente puede mostrar un tooltip o mensaje indicando "Sin datos para exportar".

### Postcondiciones
- Limpiar filtros del listado.

---

## QA_TC12_TRAMITES_EXPORT_UI - Mensaje de error comprensible cuando API falla

### Precondiciones
- Usuario autenticado.
- Se puede simular un error del backend (ej: apagar el servicio, mockear respuesta 500, o interceptar con DevTools).
- Existen trámites en el listado (`totalCount > 0`).

### Pasos
1. Navegar al módulo Trámites con datos visibles.
2. Configurar el mock o interceptor para que `GET /api/v1/traspasos/tramites/export` retorne `500 Internal Server Error`.
3. Hacer clic en el botón `Total`.
4. Esperar la respuesta del servidor.
5. Observar el mensaje mostrado al usuario.
6. Verificar la opción de reintento.
7. Quitar el mock y hacer clic en "Reintentar".

### Datos de prueba
| Campo | Valor |
|-------|-------|
| Respuesta mockeada del API | `500 Internal Server Error` |
| Mensaje de error esperado | Comprensible para el usuario (no técnico) |

### Resultado esperado
- Se muestra un mensaje de error comprensible (ej: "No se pudo generar el archivo de exportación. Intenta de nuevo.").
- El mensaje no expone detalles técnicos internos (stack trace, query SQL, etc.).
- Existe un botón o enlace "Reintentar" o similar.
- Al quitar el mock y hacer clic en "Reintentar", la descarga se completa exitosamente.
- El botón `Total` vuelve a su estado normal tras mostrar el error.

### Postcondiciones
- Eliminar el mock del interceptor de red.

---

## QA_TC13_TRAMITES_EXPORT_UI - Accesibilidad WCAG del boton de exportacion

### Precondiciones
- Usuario autenticado.
- Existen trámites en el listado.
- Herramienta de accesibilidad disponible (axe DevTools, Lighthouse o NVDA).

### Pasos
1. Navegar al módulo Trámites.
2. Ejecutar auditoría de accesibilidad sobre el componente `TramitesFilters` con axe DevTools.
3. Verificar específicamente el botón `Total` con inspección manual.
4. Navegar al botón usando solo el teclado (Tab) y activarlo con Enter/Space.
5. Verificar el contraste de color del botón en estado normal y hover.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| Estándar | WCAG 2.1 AA |
| Ratio de contraste mínimo | 4.5:1 (texto normal) |

### Resultado esperado
- axe DevTools no reporta violations en el componente `TramitesFilters`.
- El botón tiene `aria-label` descriptivo visible para lectores de pantalla.
- El botón es activable con Enter y Space desde el teclado.
- El foco visible es claro y cumple el ratio de contraste mínimo WCAG 2.1 AA (4.5:1).
- En estado `aria-busy="true"`, el lector de pantalla anuncia correctamente el estado de carga.

### Postcondiciones
- Ninguna.
