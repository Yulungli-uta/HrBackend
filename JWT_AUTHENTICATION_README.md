# Configuración de Autenticación JWT

## 📋 Resumen

Este backend HR utiliza autenticación JWT centralizada. Todos los endpoints (excepto `/health` y `/swagger`) requieren un token JWT válido emitido por el servicio de autenticación.

---

## ⚙️ Configuración Requerida

### 1. Crear archivo `appsettings.json`

Copia `appsettings.example.json` a `appsettings.json` y configura:

```json
{
  "ConnectionStrings": {
    "SqlServerConn": "Server=YOUR_SERVER;Database=YOUR_DATABASE;..."
  },
  "AuthService": {
    "Url": "http://localhost:5010",
    "ClientId": "hr-backend-app",
    "EnableCaching": true,
    "CacheDurationMinutes": 2,
    "EnableLogging": true
  }
}
```

### 2. Parámetros de Configuración

| Parámetro | Descripción | Default |
|-----------|-------------|---------|
| `AuthService:Url` | URL del servicio de autenticación | `http://localhost:5010` |
| `AuthService:ClientId` | ID del cliente para validación | `hr-backend-app` |
| `AuthService:EnableCaching` | Habilita cache de tokens | `true` |
| `AuthService:CacheDurationMinutes` | Duración del cache | `2` minutos |
| `AuthService:EnableLogging` | Habilita logs detallados | `true` |
| `AuthService:PublicPaths` | Rutas sin autenticación | `["/health", "/swagger"]` |

---

## 🔐 Cómo Funciona

1. El cliente envía una petición con header `Authorization: Bearer <token>`
2. El middleware `JwtAuthenticationMiddleware` extrae el token
3. El servicio `TokenValidationService` valida el token contra el Auth Service
4. Si es válido, la petición continúa; si no, retorna 401

---

## 🚀 Uso

### Obtener Token

```bash
curl -X POST http://localhost:5010/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'
```

Respuesta:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "...",
    "expiresIn": 3600
  }
}
```

### Usar Token

```bash
curl -X GET http://localhost:5000/api/v1/rh/departments \
  -H "Authorization: Bearer eyJhbGc..."
```

---

## 🧪 Endpoints Públicos

Los siguientes endpoints NO requieren autenticación:

- `GET /health` - Health check
- `GET /swagger` - Documentación API
- `GET /api/v1/rh/public/*` - Endpoints públicos personalizados

---

## 📊 Performance

### Caching de Tokens

El sistema cachea tokens validados para mejorar performance:

- **Primera validación**: ~50-100ms (llamada HTTP al Auth Service)
- **Validaciones subsecuentes**: ~1-2ms (cache hit)
- **Duración del cache**: Configurable (default 2 minutos)

### Recomendaciones

- **Desarrollo**: Cache 1-2 minutos, logs habilitados
- **Producción**: Cache 3-5 minutos, logs deshabilitados

---

## 🔍 Debugging

### Habilitar Logs Detallados

En `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "WsUtaSystem": "Debug"
    }
  },
  "AuthService": {
    "EnableLogging": true
  }
}
```

### Logs Generados

```
[Information] Validating token against auth service at http://localhost:5010
[Information] Token validated successfully for user john@example.com
[Information] Request to /api/v1/rh/departments authorized for user john@example.com
```

---

## 🛠️ Uso en Controladores

### Obtener Información del Usuario

```csharp
[HttpGet]
public async Task<IActionResult> GetAll()
{
    // Obtener información del usuario autenticado
    var userId = HttpContext.GetUserId();
    var email = HttpContext.GetUserEmail();
    var roles = HttpContext.GetUserRoles();
    
    // Verificar rol
    if (HttpContext.HasRole("Admin"))
    {
        // Lógica para administradores
    }
    
    // ... resto del código
}
```

---

## ⚠️ Errores Comunes

### 401 Unauthorized

**Causa**: Token no proporcionado, inválido o expirado

**Solución**:
1. Verificar que el header `Authorization` esté presente
2. Verificar formato: `Bearer <token>`
3. Obtener un nuevo token si expiró

### 503 Service Unavailable

**Causa**: El Auth Service no está disponible

**Solución**:
1. Verificar que el Auth Service esté corriendo
2. Verificar la URL en `appsettings.json`
3. Verificar conectividad de red

---

## 📝 Variables de Entorno (Opcional)

Puedes sobrescribir configuración usando variables de entorno:

```bash
export AuthService__Url="https://auth.production.com"
export AuthService__ClientId="hr-backend-prod"
export AuthService__EnableLogging="false"
```

---

## 🔗 Recursos

- Servicio de Autenticación: https://github.com/Yulungli-uta/RepositoryUta
- Frontend: https://github.com/Yulungli-uta/HrFrontend
- Documentación completa: Ver `IMPLEMENTACION_JWT_COMPLETA.md`

---

**Implementado**: Noviembre 2025
