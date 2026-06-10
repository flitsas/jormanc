# Test Cases — US #8985 [BACKEND] Tramites - Endpoint export Excel con filtros

**Feature padre:** #8983 — [TRAMITES] Exportar total de tramites a Excel  
**HU:** #8985 — [BACKEND] Endpoint export Excel con filtros de dashboard  
**Story Points:** 5  
**Módulo:** TRAMITES  
**Generado:** 2026-05-26  

---

## QA_TC01_TRAMITES_EXPORT_API - Export exitoso con filtros aplicados

### Precondiciones
- Ambiente DEV/QA levantado y base de datos con al menos 5 trámites en estado `Draft` y 5 en `Approved`.
- El endpoint `GET /api/v1/traspasos/tramites/export` está desplegado.
- Token de autenticación válido disponible.

### Pasos
1. Autenticarse en el sistema y obtener token JWT válido.
2. Enviar `GET /api/v1/traspasos/tramites/export?status=Draft&search=` con el header `Authorization: Bearer <token>`.
3. Verificar el código de respuesta HTTP.
4. Verificar el `Content-Type` de la respuesta.
5. Guardar el body de la respuesta como archivo binario.
6. Abrir el archivo con una librería XLSX y verificar que es un archivo válido.
7. Comparar la cantidad de filas del archivo con el `totalCount` del endpoint de listado con los mismos filtros.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| status | `Draft` |
| search | `` (vacío) |
| Mínimo de trámites en BD | 5 en estado Draft |

### Resultado esperado
- Código HTTP: `200 OK`
- `Content-Type`: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (o `application/octet-stream`)
- El cuerpo es un archivo `.xlsx` válido y no corrupto.
- El número de filas de datos (sin encabezado) es igual al `totalCount` del listado con `status=Draft`.

### Postcondiciones
- Ningún archivo temporal debe quedar en el servidor.

---

## QA_TC02_TRAMITES_EXPORT_API - Columnas del Excel segun especificacion

### Precondiciones
- Al menos 1 trámite en base de datos.
- Endpoint de export disponible y autenticado.

### Pasos
1. Enviar `GET /api/v1/traspasos/tramites/export` sin filtros adicionales.
2. Obtener el archivo `.xlsx` de la respuesta.
3. Leer los encabezados de la primera fila (fila 1) de la primera hoja.
4. Comparar los encabezados obtenidos con la lista especificada.
5. Leer la segunda fila (primer dato) y verificar que los valores corresponden a campos de un trámite real.

### Datos de prueba
| Campo | Valor esperado |
|-------|---------------|
| Encabezados exactos (en orden) | `id trámite`, `trámite`, `propietario`, `compañía`, `creado`, `modificado`, `secretaria`, `estado` |
| Número de columnas | 8 |

### Resultado esperado
- La primera hoja del archivo contiene exactamente 8 columnas con los encabezados en el orden especificado.
- Los datos de la primera fila corresponden a un trámite real de la base de datos (compositeId, procedureName, ownerName, companyName, createdAt, updatedAt, secretaryName, estado en español).
- El campo `estado` muestra la etiqueta en español alineada con la UI (ej: "Borrador", "Aprobado").

### Postcondiciones
- Ninguna.

---

## QA_TC03_TRAMITES_EXPORT_API - Filtro por estado devuelve solo tramites del estado solicitado

### Precondiciones
- Existen trámites en al menos dos estados distintos: `Draft` y `Approved`.
- El listado con `status=Draft` devuelve un totalCount conocido (ej: 7).

### Pasos
1. Consultar `GET /api/v1/traspasos/tramites?status=Draft` y anotar `totalCount` (ej: 7).
2. Solicitar `GET /api/v1/traspasos/tramites/export?status=Draft`.
3. Abrir el archivo `.xlsx` descargado.
4. Contar las filas de datos.
5. Revisar la columna `estado` de cada fila.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| status | `Draft` |
| totalCount esperado | 7 (o el real en BD) |

### Resultado esperado
- El número de filas de datos en el Excel es igual al `totalCount` obtenido en el paso 1.
- Todas las filas tienen el campo `estado` igual a "Borrador" (etiqueta en español de `Draft`).
- No aparece ningún trámite con estado distinto de `Draft`.

### Postcondiciones
- Ninguna.

---

## QA_TC04_TRAMITES_EXPORT_API - Limite de filas excedido devuelve error controlado

### Precondiciones
- El valor de `MaxExportRows` en configuración está establecido en un valor bajo para prueba (ej: 10).
- Existen más de 10 trámites en la base de datos que coincidan con los filtros aplicados.

### Pasos
1. Enviar `GET /api/v1/traspasos/tramites/export` sin filtros (todos los trámites).
2. Verificar el código de respuesta HTTP.
3. Verificar el cuerpo de la respuesta.
4. Comprobar que no se genera ningún archivo `.xlsx` parcial.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| MaxExportRows (config) | 10 |
| Trámites en BD sin filtro | > 10 |

### Resultado esperado
- Código HTTP: `422 Unprocessable Entity` (o `400 Bad Request` según diseño).
- El cuerpo JSON contiene un mensaje claro indicando que se superó el límite de exportación (ej: `"El número de registros supera el límite permitido de exportación"`).
- No se devuelve ningún archivo binario ni archivo parcial.
- No se generan archivos temporales en el servidor.

### Postcondiciones
- Restaurar `MaxExportRows` al valor original de configuración.

---

## QA_TC05_TRAMITES_EXPORT_API - Sin resultados para los filtros aplicados

### Precondiciones
- No existen trámites con el texto de búsqueda aplicado (ej: `search=ZZZNORESULTS999`).

### Pasos
1. Enviar `GET /api/v1/traspasos/tramites/export?search=ZZZNORESULTS999`.
2. Verificar el código de respuesta HTTP.
3. Verificar el cuerpo de la respuesta.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| search | `ZZZNORESULTS999` |
| Trámites esperados en BD | 0 |

### Resultado esperado
- Código HTTP: `404 Not Found` o `422 Unprocessable Entity`.
- El cuerpo JSON contiene un mensaje indicando que no hay datos para exportar (ej: `"No hay trámites que coincidan con los filtros para exportar"`).
- No se descarga ningún archivo Excel vacío.

### Postcondiciones
- Ninguna.

---

## QA_TC06_TRAMITES_EXPORT_API - Export sin autenticacion es rechazado

### Precondiciones
- El endpoint de export está disponible.

### Pasos
1. Enviar `GET /api/v1/traspasos/tramites/export` sin header `Authorization`.
2. Verificar el código de respuesta HTTP.
3. Verificar que no se devuelve ningún archivo en el cuerpo.

### Datos de prueba
| Campo | Valor |
|-------|-------|
| Authorization header | Ausente |

### Resultado esperado
- Código HTTP: `401 Unauthorized`.
- No se devuelve ningún archivo `.xlsx` ni datos de trámites.
- El cuerpo contiene el mensaje de error de autenticación estándar de la API.

### Postcondiciones
- Ninguna.
