# ADR-0001: Clean Architecture para backend Node.js

**Fecha**: 2024-01-15
**Status**: Aceptado
**Deciders**: Líder Técnico FLIT
**Tags**: arquitectura, backend, estructura

## Contexto

El equipo FLIT necesita una estructura de código backend que facilite el testing, la mantenibilidad a largo plazo, y la incorporación de nuevos desarrolladores. El stack es Node.js 22 + TypeScript.

## Decisión

Adoptar Clean Architecture con 4 capas estrictas: domain → application → infrastructure → interfaces.

## Alternativas consideradas

### Opción 1: Clean Architecture (4 capas)
**Pros:**
- Dominio completamente independiente del framework
- Use cases testeables sin infraestructura
- Sustitución de ORM/framework sin tocar dominio
**Cons:**
- Mayor cantidad de archivos inicialmente
- Curva de aprendizaje inicial para el equipo
**Esfuerzo**: M

### Opción 2: MVC con Express/Fastify
**Pros:**
- Familiar para la mayoría
- Menos archivos para funcionalidades simples
**Cons:**
- Lógica de negocio mezclada con HTTP
- Tests requieren levantar servidor
- Difícil mantener a medida que crece el proyecto
**Esfuerzo**: S

### Opción 3: Hexagonal (Ports & Adapters)
**Pros:**
- Completamente agnóstico de infraestructura
**Cons:**
- Más abstracto que Clean Architecture
- Overhead de setup significativo para el tamaño del equipo
**Esfuerzo**: L

## Tradeoff aceptado

Elegimos Clean Architecture sobre MVC porque la deuda técnica del MVC se acumula rápidamente en proyectos con múltiples módulos y rotación de equipo. El costo inicial de más archivos se compensa con la mantenibilidad y la capacidad de testear use cases de forma aislada.

## Consecuencias

### Lo que se gana
- Use cases testeables con mocks simples
- Dominio legible sin ruido de framework

### Lo que se pierde
- 3-4 archivos por feature vs 1-2 en MVC
- Primera semana de curva de aprendizaje

### Lo que cambia operacionalmente
- El Architecture Agent siempre propone la lista de archivos separada por capa
- Code Review valida que no se rompan las dependencias entre capas
