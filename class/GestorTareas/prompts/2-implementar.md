# Implementar Gestión de Tareas

Lee:
- AGENTS.md
- specs/tareas.md

Implementa el sistema utilizando el plan aprobado.

## Instrucciones

1. Crear la entidad Tarea.
2. Crear AppDbContext.
3. Configurar SQLite en appsettings.json.
4. Registrar el DbContext en Program.cs.
5. Crear TareasController.
6. Implementar todas las funcionalidades RF01-RF04.
7. Crear las vistas Razor necesarias.
8. Configurar la página principal para listar tareas.
9. Incorporar navegación.
10. Ejecutar dotnet build.

## Reglas

- Mantener MVC estándar.
- Utilizar EF Core para todas las operaciones.
- Utilizar métodos async cuando corresponda.
- Validar entradas con ModelState.
- Utilizar POST y ValidateAntiForgeryToken para las operaciones de escritura.
- No crear nuevas funcionalidades.
- No eliminar datos existentes.
- No ejecutar migraciones automáticamente.

## Resultado esperado

Al terminar, informa:

- Archivos creados.
- Archivos modificados.
- Resultado de compilación.
- Requisitos implementados.
- Comandos pendientes para crear la base de datos.
