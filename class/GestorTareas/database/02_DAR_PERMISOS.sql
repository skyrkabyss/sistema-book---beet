-- Ejecuta desde phpMyAdmin como root, después del archivo 01.
-- El usuario bb_app ya existe de BiblioBeat 2.0. No se cambian contraseñas.
CREATE DATABASE IF NOT EXISTS `bibliobeat_diagrama` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
GRANT ALL PRIVILEGES ON `bibliobeat_diagrama`.* TO 'bb_app'@'localhost';
GRANT ALL PRIVILEGES ON `bibliobeat_diagrama`.* TO 'bb_app'@'127.0.0.1';
SHOW GRANTS FOR 'bb_app'@'localhost';
SHOW GRANTS FOR 'bb_app'@'127.0.0.1';
