# Reportes de consulta vehicular

Esta carpeta contiene los reportes HTML generados por el `vehicle-query-agent` cada vez que se ejecuta el comando `/flit:consultar-vehiculo`.

## Convención de nombre

```
<id_lowercase>-<YYYYMMDD-HHmmss>.html
```

Donde `<id>` es:

- La **placa** si la consulta fue por placa (ej: `vej918-20260515-143022.html`).
- El **VIN** si la consulta fue por VIN (ej: `lrw3e7fs8tc752304-20260515-143155.html`).

## Privacidad

Los reportes contienen datos personales del propietario del vehículo. Mantenerlos en el repositorio es **decisión del equipo**:

- Si el repositorio es **privado** y solo accedido por el equipo FLIT, pueden commitearse para trazabilidad.
- Si el repositorio puede volverse público, agregar `docs/reports/vehicle-queries/*.html` a `.gitignore`.

## Generación

```text
/flit:consultar-vehiculo vin=LRW3E7FS8TC752304
/flit:consultar-vehiculo placa=VEJ918 tipoDoc=NIT numDoc=811011779
```

El agente solicita confirmación previa antes de ejecutar la consulta.
