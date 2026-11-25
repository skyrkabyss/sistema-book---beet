-- Ejecutar en phpMyAdmin como root/administrador local.
-- Reemplaza CAMBIA_ESTA_CLAVE en este archivo Y en la cadena de conexión.
CREATE DATABASE IF NOT EXISTS bibliobeat_otono CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'bb_app'@'localhost' IDENTIFIED BY 'CAMBIA_ESTA_CLAVE';
CREATE USER IF NOT EXISTS 'bb_app'@'127.0.0.1' IDENTIFIED BY 'CAMBIA_ESTA_CLAVE';
GRANT ALL PRIVILEGES ON bibliobeat_otono.* TO 'bb_app'@'localhost';
GRANT ALL PRIVILEGES ON bibliobeat_otono.* TO 'bb_app'@'127.0.0.1';
FLUSH PRIVILEGES;
-- ASP.NET Core crea las tablas al arrancar en una base nueva (EnsureCreatedAsync).
