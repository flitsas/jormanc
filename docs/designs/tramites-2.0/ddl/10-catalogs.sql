-- =====================================================================================
-- FLIT 2.0 · DDL 10 — Catálogos (schema catalogs)
-- Sin tenant_id, sin soft-delete. Lifecycle por is_active. code UNIQUE. external_refs jsonb.
-- Seeds: canónicos colombianos (DIVIPOLA, Registraduría, RUNT/CNT). Los volúmenes completos
-- (≈1.122 municipios, marcas/líneas RUNT) los carga un ETL (infra-agent / database-agent D).
-- =====================================================================================
SET search_path TO catalogs;

-- -------------------------------------------------------------------------------------
-- document_types (Registraduría) — default_person_kind habilita el enrutamiento FR-2 (#9409)
-- -------------------------------------------------------------------------------------
CREATE TABLE document_types (
  id                  uuid        PRIMARY KEY DEFAULT uuidv7(),
  code                text        NOT NULL,
  name                text        NOT NULL,
  default_person_kind text        NULL CHECK (default_person_kind IN ('natural','juridica')),
  is_active           boolean     NOT NULL DEFAULT true,
  display_order       integer     NOT NULL DEFAULT 0,
  external_refs       jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at          timestamptz NOT NULL DEFAULT now(),
  updated_at          timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_document_types_code UNIQUE (code)
);
CREATE TRIGGER tr_document_types_before_update_touch
  BEFORE UPDATE ON document_types FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();
COMMENT ON TABLE catalogs.document_types IS '@context:catalogs Tipos de documento de identidad (CC, NIT, ...).';

INSERT INTO document_types (code, name, default_person_kind, display_order) VALUES
  ('CC',   'Cédula de ciudadanía',          'natural',  1),
  ('CE',   'Cédula de extranjería',         'natural',  2),
  ('TI',   'Tarjeta de identidad',          'natural',  3),
  ('RC',   'Registro civil',                'natural',  4),
  ('PA',   'Pasaporte',                     'natural',  5),
  ('PEP',  'Permiso especial de permanencia','natural', 6),
  ('NUIP', 'Número único de identificación personal','natural', 7),
  ('NIT',  'Número de identificación tributaria','juridica', 8);

-- -------------------------------------------------------------------------------------
-- divipola_departments (DANE) — 32 departamentos + Bogotá D.C.
-- -------------------------------------------------------------------------------------
CREATE TABLE divipola_departments (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          char(2)     NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_divipola_departments_code UNIQUE (code)
);
CREATE TRIGGER tr_divipola_departments_before_update_touch
  BEFORE UPDATE ON divipola_departments FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO divipola_departments (code, name) VALUES
  ('05','Antioquia'),('08','Atlántico'),('11','Bogotá D.C.'),('13','Bolívar'),('15','Boyacá'),
  ('17','Caldas'),('18','Caquetá'),('19','Cauca'),('20','Cesar'),('23','Córdoba'),
  ('25','Cundinamarca'),('27','Chocó'),('41','Huila'),('44','La Guajira'),('47','Magdalena'),
  ('50','Meta'),('52','Nariño'),('54','Norte de Santander'),('63','Quindío'),('66','Risaralda'),
  ('68','Santander'),('70','Sucre'),('73','Tolima'),('76','Valle del Cauca'),('81','Arauca'),
  ('85','Casanare'),('86','Putumayo'),('88','San Andrés y Providencia'),('91','Amazonas'),
  ('94','Guainía'),('95','Guaviare'),('97','Vaupés'),('99','Vichada');

-- -------------------------------------------------------------------------------------
-- divipola_municipalities (DANE) — code de 5 dígitos; FK a department. Muestra representativa.
-- -------------------------------------------------------------------------------------
CREATE TABLE divipola_municipalities (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  department_id uuid        NOT NULL,
  code          char(5)     NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_divipola_municipalities_code UNIQUE (code),
  CONSTRAINT fk_divipola_municipalities_departments
    FOREIGN KEY (department_id) REFERENCES divipola_departments (id)
    ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_divipola_municipalities_department_id ON divipola_municipalities (department_id);
CREATE TRIGGER tr_divipola_municipalities_before_update_touch
  BEFORE UPDATE ON divipola_municipalities FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO divipola_municipalities (department_id, code, name)
SELECT d.id, m.code, m.name
FROM (VALUES
  ('11','11001','Bogotá D.C.'),
  ('05','05001','Medellín'),
  ('76','76001','Cali'),
  ('08','08001','Barranquilla'),
  ('13','13001','Cartagena'),
  ('68','68001','Bucaramanga'),
  ('54','54001','Cúcuta'),
  ('52','52001','Pasto'),
  ('66','66001','Pereira'),
  ('17','17001','Manizales')
) AS m(dep_code, code, name)
JOIN divipola_departments d ON d.code = m.dep_code;

-- -------------------------------------------------------------------------------------
-- vehicle_classes (CNT Art. 2)
-- -------------------------------------------------------------------------------------
CREATE TABLE vehicle_classes (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_vehicle_classes_code UNIQUE (code)
);
CREATE TRIGGER tr_vehicle_classes_before_update_touch
  BEFORE UPDATE ON vehicle_classes FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO vehicle_classes (code, name, display_order) VALUES
  ('AUTOMOVIL','Automóvil',1),('CAMIONETA','Camioneta',2),('CAMPERO','Campero',3),
  ('MOTOCICLETA','Motocicleta',4),('MOTOCARRO','Motocarro',5),('CUATRIMOTO','Cuatrimoto',6),
  ('BUS','Bus',7),('BUSETA','Buseta',8),('MICROBUS','Microbús',9),('CAMION','Camión',10),
  ('VOLQUETA','Volqueta',11),('TRACTOCAMION','Tractocamión',12),('MAQUINARIA','Maquinaria',13),
  ('REMOLQUE','Remolque',14),('SEMIREMOLQUE','Semirremolque',15);

-- -------------------------------------------------------------------------------------
-- service_classes (CNT Art. 2)
-- -------------------------------------------------------------------------------------
CREATE TABLE service_classes (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_service_classes_code UNIQUE (code)
);
CREATE TRIGGER tr_service_classes_before_update_touch
  BEFORE UPDATE ON service_classes FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO service_classes (code, name, display_order) VALUES
  ('PARTICULAR','Particular',1),('PUBLICO','Público',2),('OFICIAL','Oficial',3),
  ('DIPLOMATICO','Diplomático',4);

-- -------------------------------------------------------------------------------------
-- fuel_types (RUNT)
-- -------------------------------------------------------------------------------------
CREATE TABLE fuel_types (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_fuel_types_code UNIQUE (code)
);
CREATE TRIGGER tr_fuel_types_before_update_touch
  BEFORE UPDATE ON fuel_types FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO fuel_types (code, name, display_order) VALUES
  ('GASOLINA','Gasolina',1),('DIESEL','Diésel',2),('GNV','Gas natural vehicular',3),
  ('ELECTRICO','Eléctrico',4),('HIBRIDO','Híbrido',5),('HIDROGENO','Hidrógeno',6);

-- -------------------------------------------------------------------------------------
-- body_types (RUNT)
-- -------------------------------------------------------------------------------------
CREATE TABLE body_types (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_body_types_code UNIQUE (code)
);
CREATE TRIGGER tr_body_types_before_update_touch
  BEFORE UPDATE ON body_types FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO body_types (code, name, display_order) VALUES
  ('SEDAN','Sedán',1),('HATCHBACK','Hatchback',2),('SUV','SUV',3),('COUPE','Coupé',4),
  ('PICKUP','Pick-up',5),('PANEL','Panel',6),('ESTACAS','Estacas',7),('FURGON','Furgón',8);

-- -------------------------------------------------------------------------------------
-- colors (RUNT)
-- -------------------------------------------------------------------------------------
CREATE TABLE colors (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_colors_code UNIQUE (code)
);
CREATE TRIGGER tr_colors_before_update_touch
  BEFORE UPDATE ON colors FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO colors (code, name, display_order) VALUES
  ('BLANCO','Blanco',1),('NEGRO','Negro',2),('GRIS','Gris',3),('PLATA','Plata',4),
  ('ROJO','Rojo',5),('AZUL','Azul',6),('VERDE','Verde',7),('AMARILLO','Amarillo',8),
  ('NARANJA','Naranja',9),('VINOTINTO','Vinotinto',10);

-- -------------------------------------------------------------------------------------
-- vehicle_makes (RUNT). Muestra (incluye TESLA para escenarios de reglas #9410).
-- -------------------------------------------------------------------------------------
CREATE TABLE vehicle_makes (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_vehicle_makes_code UNIQUE (code)
);
CREATE TRIGGER tr_vehicle_makes_before_update_touch
  BEFORE UPDATE ON vehicle_makes FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO vehicle_makes (code, name, display_order) VALUES
  ('CHEVROLET','Chevrolet',1),('RENAULT','Renault',2),('MAZDA','Mazda',3),('TOYOTA','Toyota',4),
  ('KIA','Kia',5),('NISSAN','Nissan',6),('FORD','Ford',7),('VOLKSWAGEN','Volkswagen',8),
  ('BMW','BMW',9),('MERCEDES','Mercedes-Benz',10),('TESLA','Tesla',11),('YAMAHA','Yamaha',12),
  ('HONDA','Honda',13),('BAJAJ','Bajaj',14),('AKT','AKT',15),('SUZUKI','Suzuki',16);

-- -------------------------------------------------------------------------------------
-- vehicle_lines (RUNT) — FK a make. UNIQUE por (make_id, code).
-- -------------------------------------------------------------------------------------
CREATE TABLE vehicle_lines (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  make_id       uuid        NOT NULL,
  code          text        NOT NULL,
  name          text        NOT NULL,
  is_active     boolean     NOT NULL DEFAULT true,
  display_order integer     NOT NULL DEFAULT 0,
  external_refs jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_vehicle_lines_make_id_code UNIQUE (make_id, code),
  CONSTRAINT fk_vehicle_lines_vehicle_makes
    FOREIGN KEY (make_id) REFERENCES vehicle_makes (id)
    ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_vehicle_lines_make_id ON vehicle_lines (make_id);
CREATE TRIGGER tr_vehicle_lines_before_update_touch
  BEFORE UPDATE ON vehicle_lines FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();

INSERT INTO vehicle_lines (make_id, code, name)
SELECT mk.id, l.code, l.name
FROM (VALUES
  ('CHEVROLET','SPARK','Spark'),
  ('CHEVROLET','ONIX','Onix'),
  ('RENAULT','LOGAN','Logan'),
  ('RENAULT','SANDERO','Sandero'),
  ('MAZDA','CX30','CX-30'),
  ('TESLA','MODEL3','Model 3'),
  ('YAMAHA','FZ','FZ'),
  ('BAJAJ','BOXER','Boxer')
) AS l(make_code, code, name)
JOIN vehicle_makes mk ON mk.code = l.make_code;

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS catalogs.vehicle_lines, catalogs.vehicle_makes, catalogs.colors,
--   catalogs.body_types, catalogs.fuel_types, catalogs.service_classes, catalogs.vehicle_classes,
--   catalogs.divipola_municipalities, catalogs.divipola_departments, catalogs.document_types CASCADE;
