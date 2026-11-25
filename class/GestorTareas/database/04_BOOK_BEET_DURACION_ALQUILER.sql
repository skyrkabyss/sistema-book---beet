-- BOOK & BEET 7: Ejecutar UNA VEZ sobre la base original.
-- Antes: phpMyAdmin > bibliobeat_diagrama > Exportar > SQL.
-- NO borra registros, NO cambia nombres de tablas ni credenciales.
USE bibliobeat_diagrama;
ALTER TABLE alquiler_libros
  ADD COLUMN IF NOT EXISTS duracion_dias INT NOT NULL DEFAULT 7;
-- Los alquileres históricos conservan el plazo base de siete días.
SELECT COUNT(*) AS alquileres, MIN(duracion_dias) AS duracion_minima, MAX(duracion_dias) AS duracion_maxima
FROM alquiler_libros;
