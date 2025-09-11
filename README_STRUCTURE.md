# WsUtaSystem (.NET 9) – Estructura por capas

- Application/
  - Common/Interfaces → IRepository<,>, IService<,>
  - Common/Services   → Service<TEntity,TKey>
  - Interfaces/Repositories → I{Entidad}Repository
  - Interfaces/Services     → I{Entidad}Service
  - DTOs/{Entidad} → {Entidad}Dto, {Entidad}CreateDto, {Entidad}UpdateDto
  - Mapping/EntityToDtoProfile.cs
  - Validation/*Validator.cs (si aplica)
- Infrastructure/
  - Common/ServiceAwareEfRepository.cs
  - Repositories/{Entidad}Repository.cs
- Controllers/ (rutas sin cambios)
- Data/AppDbContext.cs (incluye ConfigureConventions)
- Filters/ValidateModelFilter.cs
- Middleware/ErrorHandlingMiddleware.cs
- Program.cs (CORS, AutoMapper, DI, Swagger)
- appsettings.json
- Directory.Build.props

> Abre WsUtaSystem.csproj en Visual Studio 2022 (SDK .NET 9).
