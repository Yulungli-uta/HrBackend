# Mapa de Arquitectura — HrBackend

## Flujo de una request HTTP

```
Cliente HTTP
    │
    ▼
[Middleware JWT]  ←── Valida token contra RepositoryUta
    │
    ▼
[Controller]
 - Recibe y valida DTO de entrada (DataAnnotations o FluentValidation)
 - Llama al Service correspondiente
 - Retorna ActionResult con el DTO de respuesta
    │
    ▼
[Service]
 - Contiene toda la lógica de negocio
 - Orquesta uno o más Repositories
 - Lanza excepciones de dominio si hay violaciones de regla
    │
    ▼
[Repository]
 - EF Core → operaciones CRUD, guardado de entidades
 - Dapper  → consultas SQL complejas, reportes, proyecciones
    │
    ▼
[SQL Server]
```

## Cuándo usar EF Core vs Dapper

| Escenario | Usar |
|---|---|
| Insertar / actualizar / eliminar entidad | EF Core |
| Consulta simple por ID o lista | EF Core |
| JOIN entre 3+ tablas | Dapper |
| Reporte con agrupaciones / sumas | Dapper |
| Procedimiento almacenado | Dapper |

## Integración con RepositoryUta (Auth)
- RepositoryUta expone endpoint de validación JWT
- HrBackend consume ese endpoint en el middleware de autenticación
- NO duplicar lógica de validación de tokens en HrBackend
- El claim del usuario autenticado se extrae del HttpContext en el Controller

## Patrones de inyección en Program.cs
```csharp
// Services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Repositories
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// DbContext (EF Core)
builder.Services.AddDbContext<HrDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
