-- ============================================================================
-- BIBLIOBEAT 4.0 | BASE COMPLETA MODIFICADA | phpMyAdmin / XAMPP MariaDB 10.4+
-- ============================================================================
-- Incluye las 15 tablas del diagrama con relaciones, login, playlists, alquiler,
-- pagos, datos musicales y libro.url_lectura + libro.archivo_pdf.
-- UNA SOLA IMPORTACION como root desde la pestaña "Importar" de phpMyAdmin.
-- Funciona sobre base NUEVA y sobre bibliobeat_diagrama ya existente.
-- NO incluye DROP, TRUNCATE ni DELETE; no elimina datos registrados.
-- Haz un respaldo antes de importar cualquier actualizacion.
-- IMPORTANTE: 'libros' no guarda bytes de PDF en MySQL. Solo la URL y el
-- nombre privado; los PDFs autorizados los gestiona el servidor ASP.NET.
-- No crea el usuario bb_app ni cambia contrasenas.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS `bibliobeat_diagrama`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `bibliobeat_diagrama`;

-- Tablas base (sin relaciones entrantes requeridas para crearlas)
CREATE TABLE IF NOT EXISTS `usuarios` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(50) NOT NULL,
  `apellido` VARCHAR(50) NOT NULL,
  `correo` VARCHAR(100) NOT NULL,
  `telefono` VARCHAR(20) NULL,
  `fecha_registro` DATE NOT NULL,
  `rol` VARCHAR(20) NOT NULL,
  `UserName` VARCHAR(256) NULL,
  `NormalizedUserName` VARCHAR(256) NULL,
  `NormalizedEmail` VARCHAR(256) NULL,
  `EmailConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
  `PasswordHash` LONGTEXT NULL,
  `SecurityStamp` LONGTEXT NULL,
  `ConcurrencyStamp` LONGTEXT NULL,
  `PhoneNumberConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
  `TwoFactorEnabled` TINYINT(1) NOT NULL DEFAULT 0,
  `LockoutEnd` DATETIME(6) NULL,
  `LockoutEnabled` TINYINT(1) NOT NULL DEFAULT 0,
  `AccessFailedCount` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_usuarios_correo` (`correo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `generos_musica` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(45) NOT NULL,
  `descripcion` TEXT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `categorias_libro` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(45) NOT NULL,
  `descripcion` TEXT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `editoriales` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(100) NOT NULL,
  `pais` VARCHAR(45) NULL,
  `correo` VARCHAR(100) NULL,
  `telefono` VARCHAR(20) NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Canciones, autores y libros
CREATE TABLE IF NOT EXISTS `canciones` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `titulo` VARCHAR(110) NOT NULL,
  `duracion` TIME NULL,
  `anio_lanzamiento` INT NULL,
  `letra` TEXT NULL,
  `genero_id` INT NOT NULL,
  `artista` VARCHAR(100) NOT NULL DEFAULT '',
  `genero` VARCHAR(50) NOT NULL DEFAULT '',
  `audio_url` VARCHAR(255) NULL,
  `fuente` VARCHAR(20) NOT NULL DEFAULT 'Local',
  `id_externo` VARCHAR(255) NULL,
  `enlace_origen` VARCHAR(800) NULL,
  `licencia` VARCHAR(255) NULL,
  `activa` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  INDEX `idx_canciones_genero` (`genero_id`),
  CONSTRAINT `fk_canciones_generos` FOREIGN KEY (`genero_id`)
    REFERENCES `generos_musica` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `autores` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(45) NOT NULL,
  `apellido` VARCHAR(45) NOT NULL,
  `nacionalidad` VARCHAR(45) NULL,
  `fecha_nacimiento` DATE NULL,
  `editorial_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_autores_editorial` (`editorial_id`),
  CONSTRAINT `fk_autores_editoriales` FOREIGN KEY (`editorial_id`)
    REFERENCES `editoriales` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `libros` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `titulo` VARCHAR(120) NOT NULL,
  `descripcion` TEXT NULL,
  `anio_publicacion` INT NULL,
  `estado` VARCHAR(20) NOT NULL,
  `categoria_id` INT NOT NULL,
  `autor_id` INT NOT NULL,
  `autor` VARCHAR(100) NOT NULL DEFAULT '',
  `genero` VARCHAR(50) NOT NULL DEFAULT '',
  `precio_renta` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `ruta_lectura` VARCHAR(160) NULL,
  `url_lectura` VARCHAR(2048) NULL COMMENT 'URL HTTPS o ruta interna autorizada',
  `archivo_pdf` VARCHAR(160) NULL COMMENT 'Nombre interno de PDF privado',
  `derechos` VARCHAR(100) NULL,
  `activo` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  INDEX `idx_libros_categoria` (`categoria_id`),
  INDEX `idx_libros_autor` (`autor_id`),
  CONSTRAINT `fk_libros_categorias` FOREIGN KEY (`categoria_id`)
    REFERENCES `categorias_libro` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT `fk_libros_autores` FOREIGN KEY (`autor_id`)
    REFERENCES `autores` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Playlists y reservas
CREATE TABLE IF NOT EXISTS `playlist` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(100) NOT NULL,
  `descripcion` TEXT NULL,
  `fecha_creacion` DATE NOT NULL,
  `privacidad` VARCHAR(20) NOT NULL,
  `usuario_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_playlist_usuario` (`usuario_id`),
  CONSTRAINT `fk_playlist_usuarios` FOREIGN KEY (`usuario_id`)
    REFERENCES `usuarios` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `reservas` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `fecha_reserva` DATE NOT NULL,
  `usuario_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_reservas_usuario` (`usuario_id`),
  CONSTRAINT `fk_reservas_usuarios` FOREIGN KEY (`usuario_id`)
    REFERENCES `usuarios` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tablas dependientes / enlaces
CREATE TABLE IF NOT EXISTS `me_gusta` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `fecha` DATETIME NOT NULL,
  `usuario_id` INT NOT NULL,
  `cancion_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_me_gusta_usuario` (`usuario_id`),
  INDEX `idx_me_gusta_cancion` (`cancion_id`),
  CONSTRAINT `fk_me_gusta_usuarios` FOREIGN KEY (`usuario_id`)
    REFERENCES `usuarios` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_me_gusta_canciones` FOREIGN KEY (`cancion_id`)
    REFERENCES `canciones` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `playlist_cancion` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `orden` INT NOT NULL,
  `fecha_agregada` DATE NOT NULL,
  `playlist_id` INT NOT NULL,
  `cancion_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_pc_playlist` (`playlist_id`),
  INDEX `idx_pc_cancion` (`cancion_id`),
  CONSTRAINT `fk_pc_playlist` FOREIGN KEY (`playlist_id`)
    REFERENCES `playlist` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_pc_canciones` FOREIGN KEY (`cancion_id`)
    REFERENCES `canciones` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `comentarios` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `texto` TEXT NOT NULL,
  `fecha` DATETIME NOT NULL,
  `usuario_id` INT NOT NULL,
  `libro_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_comentarios_usuario` (`usuario_id`),
  INDEX `idx_comentarios_libro` (`libro_id`),
  CONSTRAINT `fk_comentarios_usuarios` FOREIGN KEY (`usuario_id`)
    REFERENCES `usuarios` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_comentarios_libros` FOREIGN KEY (`libro_id`)
    REFERENCES `libros` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `alquiler_libros` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `fecha_alquilada` DATE NOT NULL,
  `fecha_devolucion` DATE NULL,
  `estado` VARCHAR(20) NOT NULL,
  `usuario_id` INT NOT NULL,
  `libro_id` INT NOT NULL,
  `creado_en` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `inicio_en` DATETIME(6) NULL,
  `vence_en` DATETIME(6) NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_alquiler_usuario` (`usuario_id`),
  INDEX `idx_alquiler_libro` (`libro_id`),
  CONSTRAINT `fk_alquiler_usuarios` FOREIGN KEY (`usuario_id`)
    REFERENCES `usuarios` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT `fk_alquiler_libros` FOREIGN KEY (`libro_id`)
    REFERENCES `libros` (`id`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `valoraciones` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `puntuacion` INT NOT NULL,
  `comentario` TEXT NULL,
  `fecha` DATE NOT NULL,
  `usuario_id` INT NOT NULL,
  `libro_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `idx_valoraciones_usuario` (`usuario_id`),
  INDEX `idx_valoraciones_libro` (`libro_id`),
  CONSTRAINT `fk_valoraciones_usuarios` FOREIGN KEY (`usuario_id`)
    REFERENCES `usuarios` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_valoraciones_libros` FOREIGN KEY (`libro_id`)
    REFERENCES `libros` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- El diagrama original no señala PK para esta tabla puente.
-- Se usa clave PRIMARIA compuesta para evitar pares duplicados.
CREATE TABLE IF NOT EXISTS `libros_has_reservas` (
  `libro_id` INT NOT NULL,
  `reserva_id` INT NOT NULL,
  PRIMARY KEY (`libro_id`, `reserva_id`),
  INDEX `idx_lhr_reserva` (`reserva_id`),
  CONSTRAINT `fk_lhr_libros` FOREIGN KEY (`libro_id`)
    REFERENCES `libros` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_lhr_reservas` FOREIGN KEY (`reserva_id`)
    REFERENCES `reservas` (`id`)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- ACTUALIZACIONES COMPATIBLES CON UNA BASE YA IMPORTADA (no destructivas)
-- En una base nueva no duplican columnas; MariaDB ignora ADD IF NOT EXISTS.
-- ============================================================================
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

-- BIBLIOBEAT 4.0: enlaces de lectura y archivos PDF privados
ALTER TABLE `libros`
  ADD COLUMN IF NOT EXISTS `url_lectura` VARCHAR(2048) NULL COMMENT 'URL de lectura HTTPS o ruta interna del lector de BiblioBeat',
  ADD COLUMN IF NOT EXISTS `archivo_pdf` VARCHAR(160) NULL COMMENT 'Identificador del PDF privado en el servidor';

-- VERIFICACION FINAL (sin depender de la base activa en phpMyAdmin)
SELECT TABLE_NAME FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'bibliobeat_diagrama' ORDER BY TABLE_NAME;
SELECT COLUMN_NAME, COLUMN_TYPE FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'bibliobeat_diagrama' AND TABLE_NAME = 'libros'
AND COLUMN_NAME IN ('url_lectura', 'archivo_pdf') ORDER BY COLUMN_NAME;
SELECT COLUMN_NAME, COLUMN_TYPE FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'bibliobeat_diagrama' AND TABLE_NAME = 'usuarios'
AND COLUMN_NAME = 'PasswordHash';
