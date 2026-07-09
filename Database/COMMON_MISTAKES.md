# Errores Frecuentes — HrBackend

## 1. Mezclar EF Core y Dapper en la misma transacción sin compartir conexión
**Problema:** EF Core y Dapper usan conexiones distintas por defecto → inconsistencia transaccional.
**Solución:**
```csharp
// Pasar la conexión de EF Core a Dapper
var connection = _context.Database.GetDbConnection();
await connection.OpenAsync();
var result = await connection.QueryAsync<T>(sql, parameters);
```

## 2. Lógica de negocio en Controllers
**Problema:** Valida reglas de negocio o hace cálculos directamente en el Controller.
**Solución:** Todo cálculo, validación de dominio y orquestación va en el Service. El Controller solo mapea y delega.

## 3. Retornar entidades EF Core directamente desde el Controller
**Problema:** Expone propiedades de navegación → loops de serialización, datos sensibles expuestos.
**Solución:** Siempre mapear a un DTO de respuesta antes de retornar.

## 4. No disponer el DbContext en operaciones de larga duración
**Problema:** Conexiones abiertas innecesariamente.
**Solución:** Usar `AddScoped` (no Singleton) para DbContext. Verificar que Repositories no guarden referencia al context más allá del request.

## 5. Olvidar `await` en métodos async del Repository
**Problema:** El resultado se retorna como Task no resuelta; falla silenciosa.
**Solución:** Siempre `await` en llamadas async. Activar el analyzer de Roslyn para detectarlo.

## 6. No validar el JWT expirado o revocado
**Problema:** Tokens viejos pasan la validación local de firma pero RepositoryUta los rechaza.
**Solución:** El middleware debe siempre consultar RepositoryUta para validación, no solo verificar firma local.

## 7. Migrations en rama equivocada
**Problema:** Correr `dotnet ef migrations add` en una rama feature genera conflictos en main.
**Solución:** Las migraciones solo se generan en la rama `develop` o `main` tras merge del feature.
