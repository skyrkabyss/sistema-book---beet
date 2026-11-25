-- BiblioBeat 4.0. Ejecutar DESPUÉS de 01_ADAPTAR_BIBLIOBEAT.sql.
-- IMPORTANTE: exporta una copia de bibliobeat_diagrama antes de ejecutar.
USE `bibliobeat_diagrama`;
ALTER TABLE `libros`
  ADD COLUMN IF NOT EXISTS `url_lectura` VARCHAR(2048) NULL COMMENT 'URL de lectura HTTPS o ruta interna del lector de BiblioBeat',
  ADD COLUMN IF NOT EXISTS `archivo_pdf` VARCHAR(160) NULL COMMENT 'Identificador del PDF privado en el servidor';
-- Para verificar (no elimina datos ni modifica los libros que ya tienes):
SHOW COLUMNS FROM `libros` LIKE 'url_lectura';
SHOW COLUMNS FROM `libros` LIKE 'archivo_pdf';
