# Política de Retención y Protección de Datos Personales

**Módulo:** Gestión de Empleados  
**Normativa:** Ley 1581 de 2012 (Habeas Data Colombia) — CWE-359 compliance  
**Última revisión:** 2026-05-13

---

## Datos personales almacenados

| Campo | Tabla | Clasificación | Base legal |
|-------|-------|---------------|------------|
| `nombre_completo` | `employees` | PII — Identificador | Consentimiento (Art. 6 Ley 1581) |
| `numero_identificacion` | `employees` | PII — Identificador único | Consentimiento + relación laboral |
| `fecha_nacimiento` | `employees` | PII — Dato sensible (edad) | Consentimiento |
| `genero` | `employees` | PII — Dato sensible | Consentimiento |
| `direccion` | `employees` | PII — Datos de ubicación | Consentimiento |
| `telefono` | `employees` | PII — Contacto | Consentimiento |
| `correo_electronico` | `employees` | PII — Contacto | Consentimiento |
| `salario` | `employees` | PII — Dato económico | Relación laboral |
| `consentimiento_datos_personales` | `employees` | Registro de consentimiento | — |
| `fecha_consentimiento` | `employees` | Registro de consentimiento | — |

## Finalidad del tratamiento

Los datos personales de los empleados se tratan exclusivamente para:

1. **Gestión del vínculo laboral**: liquidación de nómina, prestaciones sociales, afiliación a seguridad social.
2. **Comunicaciones internas**: notificaciones operativas y administrativas.
3. **Cumplimiento normativo**: reportes a entidades reguladoras (DIAN, Ministerio de Trabajo, ARL, EPS).
4. **Seguridad física y lógica**: control de acceso a instalaciones y sistemas.

**No se compartirán datos con terceros** salvo obligación legal o consentimiento expreso adicional del titular.

## Período de retención

| Tipo de dato | Retención activa | Retención archivo | Eliminación |
|---|---|---|---|
| Datos laborales generales | Duración del contrato | 10 años post-terminación (Art. 28 CST) | Anonimización o borrado seguro |
| Datos de contacto | Duración del contrato | 1 año post-terminación | Borrado seguro |
| Registro de consentimiento | Indefinido | — | No se elimina (evidencia de cumplimiento) |
| Datos económicos (salario) | Duración del contrato | 10 años (obligación fiscal) | Borrado seguro |

## Derechos del titular (Art. 8 Ley 1581/2012)

El empleado, como titular de sus datos, tiene derecho a:

- **Conocer**: solicitar información sobre los datos almacenados y su tratamiento.
- **Actualizar**: corregir datos inexactos o incompletos.
- **Rectificar**: modificar datos que no correspondan a la realidad.
- **Suprimir**: solicitar la eliminación cuando los datos ya no sean necesarios o el consentimiento sea revocado (salvo obligación legal de conservación).
- **Revocar el consentimiento**: en cualquier momento, sujeto a las limitaciones legales del vínculo laboral vigente.
- **Presentar quejas**: ante la Superintendencia de Industria y Comercio (SIC).

Para ejercer estos derechos, el titular debe dirigirse al **responsable de protección de datos** de la organización.

## Medidas de seguridad técnicas implementadas

- Acceso a campos PII restringido a roles autorizados (RBAC — pendiente implementación en v2).
- Campos de identificación (`numero_identificacion`, `correo_electronico`) con índice único — previene duplicación accidental.
- Contraseñas y tokens **nunca** se almacenan en la tabla `employees`.
- Logs de Pino configurados para **no registrar** campos PII en producción.
- Conexión a base de datos cifrada con TLS en entornos QA/PDN.
- Campos de consentimiento (`consentimiento_datos_personales`, `fecha_consentimiento`) inmutables post-registro — solo se pueden actualizar mediante proceso controlado documentado.

## Responsable del tratamiento

El responsable del tratamiento es la organización propietaria del sistema. Contacto para derechos de datos personales: definido en la política corporativa de privacidad (pendiente documentar en `docs/privacy-policy.md`).

## Referencias normativas

- Ley Estatutaria 1581 de 2012 — Protección de Datos Personales
- Decreto 1377 de 2013 — Reglamentación Ley 1581
- CWE-359: Exposure of Private Personal Information to an Unauthorized Actor
- OWASP Top 10 A02:2021 — Cryptographic Failures (datos en tránsito y reposo)
