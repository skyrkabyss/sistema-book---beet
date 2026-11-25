# Especificación: Gestión de Tareas

## 1. Objetivo

Desarrollar una aplicación web para administrar tareas personales.

## 2. Tecnologías

- ASP.NET Core MVC con .NET 10
- Entity Framework Core 10
- SQLite
- Vistas Razor

## 3. Modelo de datos

Entidad: Tarea

| Campo | Tipo | Restricción |
|---|---|---|
| Id | int | Clave primaria |
| Titulo | string | Obligatorio, máximo 100 |
| Descripcion | string | Opcional, máximo 500 |
| Completada | bool | Valor inicial false |

## 4. Funcionalidades

### RF01 - Listar tareas
El sistema debe mostrar las tareas almacenadas.

Cada tarea debe mostrar:
- Título
- Descripción
- Estado: Pendiente o Completada

### RF02 - Crear tarea
El usuario puede registrar una tarea.

Reglas:
- El título es obligatorio.
- El título no puede superar 100 caracteres.
- La descripción es opcional.
- Toda tarea nueva inicia como Pendiente.

### RF03 - Completar tarea
El usuario puede marcar una tarea pendiente como completada.

Una tarea completada permanece completada.

### RF04 - Eliminar tarea
El usuario puede eliminar una tarea existente.

## 5. Interfaz

- Página principal con listado de tareas.
- Botón Nueva tarea.
- Formulario para registrar tareas.
- Botón Completar para tareas pendientes.
- Botón Eliminar.
- Utilizar Bootstrap existente en MVC.

## 6. Persistencia

- Utilizar SQLite.
- Acceder a datos mediante Entity Framework Core.
- Crear el esquema mediante migraciones.
- No usar listas en memoria como almacenamiento.

## 7. Restricciones

- No implementar login.
- No implementar roles.
- No agregar funcionalidades no especificadas.
- No crear una arquitectura por capas adicional.
- Mantener la estructura MVC estándar.

## 8. Criterios de aceptación

CA01: Al iniciar, se visualiza el listado.

CA02: Al crear una tarea válida, aparece en el listado y se guarda en SQLite.

CA03: No se puede registrar una tarea con título vacío.

CA04: Una nueva tarea tiene estado Pendiente.

CA05: Al completar una tarea, su estado cambia a Completada.

CA06: Al eliminar una tarea, desaparece del listado y de SQLite.

CA07: Las tareas persisten al reiniciar la aplicación.

CA08: No se puede modificar información mediante solicitudes GET.


## 9. Cambio de especificación V2

### RF05 - Fecha límite
Cada tarea podrá tener una fecha límite.

### Modificación del modelo
Agregar a Tarea:
- FechaLimite: DateTime?
- Campo opcional.

### Interfaz
- Agregar Fecha límite al formulario.
- Mostrar Fecha límite en el listado cuando esté definida.

### Criterios de aceptación
CA09: Se puede crear una tarea sin fecha límite.
CA10: Se puede registrar una fecha límite.
CA11: La fecha límite permanece almacenada después de reiniciar la aplicación.
CA12: Las tareas anteriores siguen funcionando aunque no tengan fecha límite.

### Persistencia
Actualizar el esquema mediante una migración.
No eliminar ni recrear la base de datos.
