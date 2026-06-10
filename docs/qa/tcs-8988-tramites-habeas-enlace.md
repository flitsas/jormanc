# Test Cases — Feature #8987 / HU #8988

**Módulo:** TRAMITES  
**Alcance:** HABEAS  
**US vinculada:** #8988  
**Generado por:** QA Agent (Modo A) + `flit-test-case-generator`  
**Fecha:** 2026-05-26

---

## Matriz de cobertura

| TC | Tipo | Escenario Gherkin origen |
|----|------|--------------------------|
| QA_TC01 | Positivo | Enlace abre política en nueva pestaña |
| QA_TC02 | Positivo | Checkbox independiente del enlace |
| QA_TC03 | Borde | Clic en enlace no marca checkbox |
| QA_TC04 | Negativo | Consulta bloqueada sin aceptación |
| QA_TC05 | Positivo | Enlace seguro (rel/target) |
| QA_TC06 | Borde | URL configurable vía env |
| QA_TC07 | Borde | Wizard conserva datos tras abrir política |
| QA_TC08 | Borde | Accesibilidad teclado checkbox vs enlace |

---

# QA_TC01_TRAMITES_HABEAS - Enlace abre política en nueva pestaña

**ID:** QA_TC01  
**US vinculada:** #8988  
**Módulo:** TRAMITES  
**Tipo:** Positivo  
**Prioridad:** Alta  
**Trazabilidad Gherkin:** *Enlace abre politica en nueva pestana*

## Precondiciones

- Usuario autenticado con permiso para crear trámite.
- Navegador permite ventanas emergentes (no bloqueadas para el dominio FLIT).
- `VITE_PRIVACY_POLICY_URL` configurada con `https://flitsas.com.co/privacy-policy` (o default equivalente).
- Ruta: `/traspasos/nuevo` — paso 1 visible.

## Pasos

1. Abrir el wizard **Nuevo trámite** y ubicarse en el paso 1 (Consulta vehículo).
2. Localizar el texto/enlace «Acepto la política de privacidad y tratamiento de datos (Habeas Data).».
3. Hacer clic **solo** en el texto del enlace (no en el recuadro del checkbox).
4. Observar pestañas del navegador y URL de la nueva pestaña.
5. Volver a la pestaña del wizard y verificar paso y campos.

## Datos de prueba

| Campo | Valor |
|-------|-------|
| documento_vendedor | 900123456 |
| placa | ABC123 |
| tipo_doc | CC |

## Resultado esperado

- Se abre **una nueva pestaña** del navegador.
- La URL de la nueva pestaña es `https://flitsas.com.co/privacy-policy` (o la configurada en env).
- La pestaña del wizard **permanece en paso 1** sin recarga ni pérdida de datos ingresados.

## Postcondiciones (limpieza)

- Cerrar la pestaña de política abierta en la prueba.
- No es necesario borrador de trámite si no se consultó vehículo.

---

# QA_TC02_TRAMITES_HABEAS - Checkbox independiente del enlace

**ID:** QA_TC02  
**US vinculada:** #8988  
**Tipo:** Positivo  
**Prioridad:** Alta  
**Trazabilidad Gherkin:** *Checkbox independiente del enlace*

## Precondiciones

- Paso 1 del wizard visible.
- Checkbox Habeas Data **desmarcado** inicialmente.

## Pasos

1. Hacer clic **únicamente** en el recuadro del checkbox (no en el texto del enlace).
2. Verificar estado del checkbox.
3. Repetir clic en el recuadro del checkbox.
4. Confirmar que no se abrió pestaña de política en ningún clic.

## Datos de prueba

N/A (solo interacción UI).

## Resultado esperado

- Primer clic: checkbox **marcado**.
- Segundo clic: checkbox **desmarcado**.
- En ningún momento se abre la URL de política de privacidad.

## Postcondiciones

- Dejar checkbox en estado desmarcado para TCs siguientes si se ejecutan en secuencia manual.

---

# QA_TC03_TRAMITES_HABEAS - Clic en enlace no marca el checkbox automáticamente

**ID:** QA_TC03  
**US vinculada:** #8988  
**Tipo:** Borde  
**Prioridad:** Alta  
**Trazabilidad Gherkin:** *Clic en enlace no marca el checkbox automaticamente*

## Precondiciones

- Paso 1 visible.
- Checkbox Habeas Data **desmarcado**.

## Pasos

1. Confirmar que el checkbox está desmarcado.
2. Clic **único** en el enlace del texto de política.
3. Cerrar o ignorar la nueva pestaña de política.
4. Inspeccionar estado del checkbox en el wizard.

## Datos de prueba

N/A

## Resultado esperado

- El checkbox **sigue desmarcado** tras abrir la política.
- No hay `htmlFor` ni propagación de clic que marque el checkbox al activar el enlace.

## Postcondiciones

- Cerrar pestaña de política si quedó abierta.

---

# QA_TC04_TRAMITES_HABEAS - Consulta bloqueada sin aceptación Habeas Data

**ID:** QA_TC04  
**US vinculada:** #8988  
**Tipo:** Negativo  
**Prioridad:** Alta  
**Trazabilidad Gherkin:** *Consulta bloqueada sin aceptacion*

## Precondiciones

- Paso 1 visible.
- Campos obligatorios de consulta **completos** (documento, placa, tipo doc según reglas NIT/CC).
- Checkbox Habeas Data **desmarcado**.

## Pasos

1. Completar documento, placa y demás campos requeridos del paso 1.
2. Verificar que el botón «Consultar vehículo» está deshabilitado o intentar clic si habilitado por error.
3. Leer mensaje de ayuda bajo el botón.

## Datos de prueba

| Campo | Valor |
|-------|-------|
| documento_vendedor | 80123456 |
| placa | XYZ99A |
| tipo_doc | CC |

## Resultado esperado

- La acción de consulta **no se ejecuta** (botón `disabled` o equivalente).
- Se muestra mensaje existente del tipo «Complete … y acepte Habeas Data para consultar.»
- No se dispara llamada RUNT/Verifik.

## Postcondiciones

- Sin borrador persistido por consulta.

---

# QA_TC05_TRAMITES_HABEAS - Enlace incluye rel noopener noreferrer y target blank

**ID:** QA_TC05  
**US vinculada:** #8988  
**Tipo:** Positivo  
**Prioridad:** Media  
**Trazabilidad Gherkin:** *Enlace seguro*

## Precondiciones

- Paso 1 renderizado.
- Herramientas de desarrollador disponibles.

## Pasos

1. Inspeccionar el elemento `<a>` del enlace de política en el DOM.
2. Verificar atributos `target` y `rel`.
3. Opcional: validar en test RTL/unit que el componente renderiza esos atributos.

## Datos de prueba

N/A

## Resultado esperado

- `target="_blank"`
- `rel="noopener noreferrer"` (orden de tokens puede variar pero ambos presentes)
- Patrón alineado con `TramiteDetailHero.tsx` (enlace FUR).

## Postcondiciones

N/A

---

# QA_TC06_TRAMITES_HABEAS - URL del enlace respeta VITE_PRIVACY_POLICY_URL

**ID:** QA_TC06  
**US vinculada:** #8988  
**Tipo:** Borde  
**Prioridad:** Media  
**Trazabilidad Gherkin:** Notas técnicas US (env configurable)

## Precondiciones

- Build o entorno DEV con `VITE_PRIVACY_POLICY_URL=https://ejemplo-qa.flit.test/privacidad` (URL sintética de prueba).
- Reinicio del frontend tras cambio de env.

## Pasos

1. Abrir paso 1 con la env de prueba cargada.
2. Inspeccionar `href` del enlace de política.
3. Clic en el enlace y verificar URL en nueva pestaña.

## Datos de prueba

| Variable | Valor |
|----------|-------|
| VITE_PRIVACY_POLICY_URL | `https://ejemplo-qa.flit.test/privacidad` |

## Resultado esperado

- `href` y pestaña abierta usan la URL del env, **no** un valor hardcodeado distinto.
- Si la variable falta, fallback documentado a `https://flitsas.com.co/privacy-policy`.

## Postcondiciones

- Restaurar env por defecto en entorno compartido.

---

# QA_TC07_TRAMITES_HABEAS - Wizard conserva datos del paso 1 tras abrir política

**ID:** QA_TC07  
**US vinculada:** #8988  
**Tipo:** Borde  
**Prioridad:** Media  
**Trazabilidad Gherkin:** *la pestaña del wizard permanece en el paso 1 sin recargar* (extensión)

## Precondiciones

- Paso 1 con datos parciales ingresados.

## Pasos

1. Ingresar documento `700111222`, placa `TST01`, tipo CC.
2. Marcar checkbox Habeas Data.
3. Abrir enlace de política (nueva pestaña).
4. Volver al wizard sin recargar (F5).
5. Verificar valores de campos y paso activo.

## Datos de prueba

| Campo | Valor |
|-------|-------|
| documento | 700111222 |
| placa | TST01 |

## Resultado esperado

- Campos conservan valores ingresados.
- Sigue en paso 1 del stepper.
- Checkbox mantiene estado marcado.

## Postcondiciones

- Descartar borrador si se creó.

---

# QA_TC08_TRAMITES_HABEAS - Navegación por teclado separa checkbox y enlace

**ID:** QA_TC08  
**US vinculada:** #8988  
**Tipo:** Borde  
**Prioridad:** Baja  
**Trazabilidad Gherkin:** WCAG / notas técnicas (`aria-label`)

## Precondiciones

- Paso 1 visible.
- Navegación solo con teclado (sin mouse).

## Pasos

1. Tabular hasta el checkbox de Habeas Data.
2. Activar con Espacio — verificar toggle.
3. Tabular hasta el enlace de política.
4. Activar con Enter — verificar apertura de política.
5. Confirmar que Espacio en el enlace no marca el checkbox por error.

## Datos de prueba

N/A

## Resultado esperado

- Checkbox tiene foco visible y `aria-label` descriptivo.
- Enlace es focusable por separado.
- Controles no comparten el mismo `htmlFor` que dispare ambas acciones.

## Postcondiciones

N/A

---

## Resumen de publicación ADO

| Consecutivo | Título Task sugerido |
|-------------|---------------------|
| 01–08 | Ver títulos H1 de cada sección |

Publicar como **Task** hija de HU #8988 con trazabilidad a escenario Gherkin en descripción.
