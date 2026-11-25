# Instrucciones del proyecto

## Tecnologías

- C# y ASP.NET Core MVC .NET 10
- Entity Framework Core 10
- SQLite
- Razor y Bootstrap

## Reglas de desarrollo

1. Leer specs/tareas.md antes de implementar.
2. Respetar estrictamente los requisitos.
3. No agregar funcionalidades adicionales.
4. Mantener el código sencillo y legible.
5. Utilizar programación asíncrona en acceso a datos.
6. Utilizar Entity Framework Core para persistencia.
7. Utilizar ModelState para validar formularios.
8. Utilizar POST y antiforgery para modificar datos.
9. No modificar archivos innecesarios.
10. No eliminar migraciones existentes.

## Estructura

- Models: entidades
- Data: contexto de base de datos
- Controllers: lógica MVC
- Views: vistas Razor
- specs: especificaciones funcionales
- prompts: instrucciones de trabajo

## Proceso de trabajo

Antes de implementar:
- Revisar la especificación.
- Identificar archivos necesarios.
- Presentar un plan.

Después de implementar:
- Ejecutar dotnet build.
- Verificar criterios de aceptación.
- Informar archivos creados o modificados.
- Informar errores pendientes.
