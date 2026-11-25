-- BiblioBeat 3.0 / adaptar el modelo de 15 tablas que ya importaste.
-- REQUIERE: bibliobeat_diagrama creada con BiblioBeat_Diagrama_phpMyAdmin.sql
-- XAMPP MariaDB 10.4+ (soporta ADD COLUMN IF NOT EXISTS).
-- Antes de ejecutar: exporta una copia completa de bibliobeat_diagrama desde phpMyAdmin.
-- No elimina ni recrea ninguna de tus 15 tablas. Se puede importar dos veces.
USE `bibliobeat_diagrama`;

-- 1) ASP.NET Identity aprovecha usuarios.id / correo para que las FK del diagrama sigan funcionando.
ALTER TABLE `usuarios`
  ADD COLUMN IF NOT EXISTS `UserName` VARCHAR(256) NULL,
  ADD COLUMN IF NOT EXISTS `NormalizedUserName` VARCHAR(256) NULL,
  ADD COLUMN IF NOT EXISTS `NormalizedEmail` VARCHAR(256) NULL,
  ADD COLUMN IF NOT EXISTS `EmailConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS `PasswordHash` LONGTEXT NULL,
  ADD COLUMN IF NOT EXISTS `SecurityStamp` LONGTEXT NULL,
  ADD COLUMN IF NOT EXISTS `ConcurrencyStamp` LONGTEXT NULL,
  ADD COLUMN IF NOT EXISTS `PhoneNumberConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS `TwoFactorEnabled` TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS `LockoutEnd` DATETIME(6) NULL,
  ADD COLUMN IF NOT EXISTS `LockoutEnabled` TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS `AccessFailedCount` INT NOT NULL DEFAULT 0;

-- 2) Complementos de catálogo: se conservan autor_id/categoria_id/genero_id originales.
ALTER TABLE `libros`
  MODIFY COLUMN `titulo` VARCHAR(120) NOT NULL,
  ADD COLUMN IF NOT EXISTS `autor` VARCHAR(100) NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS `genero` VARCHAR(50) NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS `precio_renta` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS `ruta_lectura` VARCHAR(160) NULL,
  ADD COLUMN IF NOT EXISTS `derechos` VARCHAR(100) NULL,
  ADD COLUMN IF NOT EXISTS `activo` TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE `canciones`
  MODIFY COLUMN `titulo` VARCHAR(110) NOT NULL,
  ADD COLUMN IF NOT EXISTS `artista` VARCHAR(100) NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS `genero` VARCHAR(50) NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS `audio_url` VARCHAR(255) NULL,
  ADD COLUMN IF NOT EXISTS `fuente` VARCHAR(20) NOT NULL DEFAULT 'Local',
  ADD COLUMN IF NOT EXISTS `id_externo` VARCHAR(255) NULL,
  ADD COLUMN IF NOT EXISTS `enlace_origen` VARCHAR(800) NULL,
  ADD COLUMN IF NOT EXISTS `licencia` VARCHAR(255) NULL,
  ADD COLUMN IF NOT EXISTS `activa` TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE `alquiler_libros`
  ADD COLUMN IF NOT EXISTS `creado_en` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  ADD COLUMN IF NOT EXISTS `inicio_en` DATETIME(6) NULL,
  ADD COLUMN IF NOT EXISTS `vence_en` DATETIME(6) NULL;

-- Completar texto de búsqueda del catálogo importado, sin pisar valores ya escritos.
UPDATE `libros` l JOIN `autores` a ON a.id=l.autor_id
  SET l.autor=TRIM(CONCAT(a.nombre,' ',a.apellido)) WHERE l.autor='';
UPDATE `libros` l JOIN `categorias_libro` c ON c.id=l.categoria_id
  SET l.genero=c.nombre WHERE l.genero='';
UPDATE `canciones` s JOIN `generos_musica` g ON g.id=s.genero_id
  SET s.genero=g.nombre WHERE s.genero='';

-- 3) Tablas auxiliares de Identity. Roles reales independientes del texto `usuarios.rol`.
CREATE TABLE IF NOT EXISTS `AspNetRoles` (
 `Id` INT NOT NULL AUTO_INCREMENT,`Name` VARCHAR(256) NULL,`NormalizedName` VARCHAR(256) NULL,
 `ConcurrencyStamp` LONGTEXT NULL,PRIMARY KEY(`Id`),UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `AspNetUserRoles` (
 `UserId` INT NOT NULL,`RoleId` INT NOT NULL,PRIMARY KEY(`UserId`,`RoleId`),
 KEY `IX_AspNetUserRoles_RoleId`(`RoleId`),
 CONSTRAINT `fk_bb_ur_user` FOREIGN KEY(`UserId`) REFERENCES `usuarios`(`id`) ON DELETE CASCADE,
 CONSTRAINT `fk_bb_ur_role` FOREIGN KEY(`RoleId`) REFERENCES `AspNetRoles`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE IF NOT EXISTS `AspNetUserClaims` (
 `Id` INT NOT NULL AUTO_INCREMENT, `UserId` INT NOT NULL,`ClaimType` LONGTEXT NULL,`ClaimValue` LONGTEXT NULL,
 PRIMARY KEY(`Id`),KEY `IX_AspNetUserClaims_UserId`(`UserId`),
 CONSTRAINT `fk_bb_uc_user` FOREIGN KEY(`UserId`) REFERENCES `usuarios`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE IF NOT EXISTS `AspNetUserLogins` (
 `LoginProvider` VARCHAR(128) NOT NULL,`ProviderKey` VARCHAR(128) NOT NULL,`ProviderDisplayName` LONGTEXT NULL,
 `UserId` INT NOT NULL,PRIMARY KEY(`LoginProvider`,`ProviderKey`),KEY `IX_AspNetUserLogins_UserId`(`UserId`),
 CONSTRAINT `fk_bb_ul_user` FOREIGN KEY(`UserId`) REFERENCES `usuarios`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE IF NOT EXISTS `AspNetUserTokens` (
 `UserId` INT NOT NULL,`LoginProvider` VARCHAR(128) NOT NULL,`Name` VARCHAR(128) NOT NULL,`Value` LONGTEXT NULL,
 PRIMARY KEY(`UserId`,`LoginProvider`,`Name`),
 CONSTRAINT `fk_bb_ut_user` FOREIGN KEY(`UserId`) REFERENCES `usuarios`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE IF NOT EXISTS `AspNetRoleClaims` (
 `Id` INT NOT NULL AUTO_INCREMENT,`RoleId` INT NOT NULL,`ClaimType` LONGTEXT NULL,`ClaimValue` LONGTEXT NULL,
 PRIMARY KEY(`Id`),KEY `IX_AspNetRoleClaims_RoleId`(`RoleId`),
 CONSTRAINT `fk_bb_rc_role` FOREIGN KEY(`RoleId`) REFERENCES `AspNetRoles`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4) Pagos por QR vinculados a alquiler_libros; jamás habilitar lectura sin webhook válido.
CREATE TABLE IF NOT EXISTS `pagos` (
 `id` INT NOT NULL AUTO_INCREMENT, `alquiler_id` INT NOT NULL,
 `metodo` VARCHAR(20) NOT NULL DEFAULT 'QR',`estado` VARCHAR(30) NOT NULL DEFAULT 'Pendiente',
 `referencia` VARCHAR(100) NULL,`monto` DECIMAL(10,2) NOT NULL DEFAULT 0,
 `proveedor` VARCHAR(20) NULL,`transaccion_externa` VARCHAR(120) NULL,
 `qr_id` VARCHAR(80) NULL,`qr_imagen_base64` LONGTEXT NULL,`qr_vence_en` DATETIME(6) NULL,
 `fecha` DATETIME(6) NOT NULL,`verificado_en` DATETIME(6) NULL,
 PRIMARY KEY(`id`),UNIQUE KEY `uq_bb_pago_alquiler`(`alquiler_id`),
 CONSTRAINT `fk_bb_pago_alquiler` FOREIGN KEY(`alquiler_id`) REFERENCES `alquiler_libros`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5) Compatibilidad con la función antigua de tareas (solo visible para administradores).
CREATE TABLE IF NOT EXISTS `Tareas` (
 `Id` INT NOT NULL AUTO_INCREMENT,`Titulo` VARCHAR(100) NOT NULL,
 `Descripcion` VARCHAR(500) NULL,`Completada` TINYINT(1) NOT NULL DEFAULT 0,
 `FechaLimite` DATETIME(6) NULL, PRIMARY KEY(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- CONSULTAS DE VERIFICACIÓN (15 tablas originales + tablas de apoyo):
SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA='bibliobeat_diagrama' ORDER BY TABLE_NAME;
SHOW COLUMNS FROM `usuarios`;
