# WsUtaSystem – Capas + Controllers migrados a Services + DTOs

- Controllers llaman a I{Entidad}Service y devuelven DTOs.
- Rutas se preservan (clase [Route] original).
- Validación: FluentValidation (+ filtro ModelState).
- Errores: ProblemDetails via ErrorHandlingMiddleware.

## Endpoints típicos
GET     /api/{recurso}
GET     /api/{recurso}/{id}
POST    /api/{recurso}
PUT     /api/{recurso}/{id}
DELETE  /api/{recurso}/{id}
