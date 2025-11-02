# FileManagement - Configuración de Credenciales Encriptadas

## 📋 Descripción General

El servicio **FileManagement** permite subir, descargar y eliminar archivos en directorios locales o de red (NAS/SMB). Soporta dos modos de operación:

1. **Modo Directo (sin credenciales):** Para puntos de montaje locales o directorios accesibles sin autenticación
2. **Modo Impersonation (con credenciales):** Para NAS/SMB remotos que requieren autenticación con usuario y contraseña

---

## 🔐 Características de Seguridad

- ✅ **Encriptación AES-256** para credenciales
- ✅ **Windows Impersonation** opcional para acceso a NAS/SMB
- ✅ **Credenciales centralizadas** en appsettings.json
- ✅ **Clave de encriptación separada** del código
- ✅ **Prevención de path traversal**
- ✅ **Validación de extensiones y tamaños**
- ✅ **Modo configurable** (con/sin autenticación)

---

## 🚀 Configuración Paso a Paso

### **Escenario 1: Punto de Montaje Local (Sin Credenciales)**

Este es el modo más simple y recomendado cuando tienes un punto de montaje local o un directorio accesible sin autenticación.

#### **Paso 1: Configurar appsettings.json**

```json
{
  "FileManagement": {
    "UseImpersonation": false
  }
}
```

**Características:**
- ✅ No requiere credenciales
- ✅ Acceso directo al sistema de archivos
- ✅ Funciona en Windows y Linux
- ✅ Mejor rendimiento (sin overhead de autenticación)
- ✅ Más simple de configurar

#### **Paso 2: Configurar DirectoryParameters**

```sql
INSERT INTO TBL_DirectoryParameters 
(Code, PhysicalPath, RelativePath, Description, Extension, MaxSizeMB, Status, CreatedAt, CreatedBy)
VALUES 
('LOCAL_DOCS', 
 '/mnt/nas/documents',  -- Punto de montaje local
 '/docs', 
 'Documentos en punto de montaje', 
 '.pdf,.docx,.xlsx', 
 10, 
 1, 
 GETDATE(), 
 1);
```

**¡Listo!** No necesitas configurar credenciales ni clave de encriptación.

---

### **Escenario 2: NAS Remoto con Credenciales (Impersonation)**

Usa este modo cuando necesitas acceder a un NAS/SMB remoto que requiere autenticación.

#### **Paso 1: Generar Clave de Encriptación**

La clave debe tener **32 caracteres** para AES-256.

**Ejemplo de clave:**
```
MySecretKey123456789012345678
```

**Generar clave aleatoria (PowerShell):**
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```

---

#### **Paso 2: Encriptar Credenciales**

Usar la herramienta `CredentialEncryptor` para encriptar las credenciales del NAS.

**Ejecutar la herramienta:**

```bash
cd Tools
dotnet run -- --key "MySecretKey123456789012345678" --username "nas_user" --password "nas_password123" --domain "WORKGROUP"
```

**Salida esperada:**

```
===========================================
  Credential Encryptor - AES-256
===========================================

Encryption Key: MySecretKey123456789012345678

Username (plain):     nas_user
Username (encrypted): dGVzdF9lbmNyeXB0ZWRfdXNlcm5hbWU=

Password (plain):     nas_password123
Password (encrypted): dGVzdF9lbmNyeXB0ZWRfcGFzc3dvcmQ=

Domain (plain):     WORKGROUP
Domain (encrypted): dGVzdF9lbmNyeXB0ZWRfZG9tYWlu

===========================================
  Add to appsettings.json:
===========================================
{
  "FileManagement": {
    "UseImpersonation": true,
    "EncryptionKey": "MySecretKey123456789012345678",
    "NetworkCredentials": {
      "Username": "dGVzdF9lbmNyeXB0ZWRfdXNlcm5hbWU=",
      "Password": "dGVzdF9lbmNyeXB0ZWRfcGFzc3dvcmQ=",
      "Domain": "dGVzdF9lbmNyeXB0ZWRfZG9tYWlu"
    }
  }
}
```

---

#### **Paso 3: Agregar Configuración a appsettings.json**

Copiar la salida del paso anterior en `appsettings.json`:

```json
{
  "FileManagement": {
    "UseImpersonation": true,
    "EncryptionKey": "MySecretKey123456789012345678",
    "NetworkCredentials": {
      "Username": "dGVzdF9lbmNyeXB0ZWRfdXNlcm5hbWU=",
      "Password": "dGVzdF9lbmNyeXB0ZWRfcGFzc3dvcmQ=",
      "Domain": "dGVzdF9lbmNyeXB0ZWRfZG9tYWlu"
    }
  }
}
```

**Nota:** El campo `Domain` es opcional. Si no usas dominio, puedes omitirlo o dejarlo vacío.

---

#### **Paso 4: Configurar DirectoryParameters**

```sql
INSERT INTO TBL_DirectoryParameters 
(Code, PhysicalPath, RelativePath, Description, Extension, MaxSizeMB, Status, CreatedAt, CreatedBy)
VALUES 
('NAS_CONTRACTS', 
 '\\\\192.168.1.100\\shared\\contracts',  -- Ruta UNC del NAS
 '/contracts', 
 'Contratos en NAS remoto', 
 '.pdf,.docx,.xlsx', 
 10, 
 1, 
 GETDATE(), 
 1);
```

---

#### **Paso 5: Registrar Servicios en Program.cs**

Agregar las siguientes líneas en `Program.cs`:

```csharp
using WsUtaSystem.Infrastructure.Configuration;

// Configurar FileManagementSettings
builder.Services.Configure<FileManagementSettings>(
    builder.Configuration.GetSection("FileManagement"));

// Registrar servicios
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IFileManagementService, FileManagementService>();
```

---

## 📊 Comparación de Modos

| Característica | Modo Directo | Modo Impersonation |
|----------------|--------------|-------------------|
| **UseImpersonation** | `false` | `true` |
| **Requiere credenciales** | ❌ No | ✅ Sí |
| **Requiere encriptación** | ❌ No | ✅ Sí |
| **Plataforma** | Windows/Linux | Solo Windows |
| **Rendimiento** | ⚡ Rápido | 🐢 Más lento |
| **Complejidad** | 🟢 Simple | 🟡 Media |
| **Uso recomendado** | Punto de montaje local | NAS remoto con auth |

---

## 📡 Endpoints API

Los endpoints son los mismos para ambos modos:

### **1. Subir Archivo**

```http
POST /files/upload
Content-Type: multipart/form-data

FormData:
  - DirectoryCode: "LOCAL_DOCS"
  - RelativePath: "/docs/"
  - FileName: "documento.pdf"
  - File: [archivo binario]
```

### **2. Subir Múltiples Archivos**

```http
POST /files/upload-multiple
Content-Type: multipart/form-data

FormData:
  - DirectoryCode: "LOCAL_DOCS"
  - RelativePath: "/docs/"
  - Files: [archivo1.pdf, archivo2.pdf]
```

### **3. Descargar Archivo**

```http
GET /files/download/LOCAL_DOCS?filePath=/docs/2025/documento.pdf
```

### **4. Eliminar Archivo**

```http
DELETE /files/delete/LOCAL_DOCS?filePath=/docs/2025/documento.pdf
```

### **5. Verificar Existencia**

```http
GET /files/exists/LOCAL_DOCS?filePath=/docs/2025/documento.pdf
```

---

## 🔧 Cambiar Entre Modos

Para cambiar entre modo directo e impersonation, solo necesitas modificar `appsettings.json`:

### **De Modo Directo a Impersonation:**

```json
{
  "FileManagement": {
    "UseImpersonation": true,  // ← Cambiar de false a true
    "EncryptionKey": "MySecretKey123456789012345678",
    "NetworkCredentials": {
      "Username": "encrypted_username",
      "Password": "encrypted_password",
      "Domain": "encrypted_domain"
    }
  }
}
```

### **De Impersonation a Modo Directo:**

```json
{
  "FileManagement": {
    "UseImpersonation": false  // ← Cambiar de true a false
  }
}
```

**No necesitas reiniciar la aplicación**, los cambios se aplican automáticamente.

---

## 🛡️ Seguridad

### **Modo Directo:**
- ✅ Sin credenciales expuestas
- ✅ Usa permisos del usuario que ejecuta la aplicación
- ✅ Más seguro si el punto de montaje está bien configurado

### **Modo Impersonation:**
- ✅ Credenciales encriptadas con AES-256
- ✅ Clave de encriptación separada
- ✅ Impersonation solo durante operaciones de archivos
- ✅ Limpieza automática de recursos

---

## 🐛 Solución de Problemas

### **Error: "LogonUser failed with error code: 1326"**
- **Causa:** Usuario o contraseña incorrectos (solo en modo impersonation)
- **Solución:** Verificar credenciales y volver a encriptar

### **Error: "Directory with code 'XXX' not found"**
- **Causa:** No existe el registro en `TBL_DirectoryParameters`
- **Solución:** Insertar el registro con el código correcto

### **Error: "Access denied"**
- **Causa:** Permisos insuficientes en el directorio
- **Solución (Modo Directo):** Dar permisos al usuario que ejecuta la app
- **Solución (Modo Impersonation):** Verificar permisos del usuario configurado

### **Error: "PlatformNotSupportedException"**
- **Causa:** Intentando usar impersonation en Linux
- **Solución:** Cambiar `UseImpersonation` a `false`

---

## 📝 Ejemplos Completos

### **Ejemplo 1: Configuración Mínima (Modo Directo)**

**appsettings.json:**
```json
{
  "FileManagement": {
    "UseImpersonation": false
  }
}
```

**SQL:**
```sql
INSERT INTO TBL_DirectoryParameters VALUES 
('DOCS', '/mnt/nas/docs', '/docs', 'Documentos', '.pdf', 5, 1, GETDATE(), 1, NULL, NULL);
```

**Listo para usar!**

---

### **Ejemplo 2: Configuración Completa (Modo Impersonation)**

**1. Encriptar:**
```bash
dotnet run --project Tools/CredentialEncryptor.cs -- --key "MyKey12345678901234567890123" --username "admin" --password "P@ssw0rd"
```

**2. appsettings.json:**
```json
{
  "FileManagement": {
    "UseImpersonation": true,
    "EncryptionKey": "MyKey12345678901234567890123",
    "NetworkCredentials": {
      "Username": "encrypted_value_here",
      "Password": "encrypted_value_here"
    }
  }
}
```

**3. SQL:**
```sql
INSERT INTO TBL_DirectoryParameters VALUES 
('NAS', '\\\\nas\\share', '/files', 'NAS Files', '.pdf,.docx', 10, 1, GETDATE(), 1, NULL, NULL);
```

**4. Program.cs:**
```csharp
builder.Services.Configure<FileManagementSettings>(
    builder.Configuration.GetSection("FileManagement"));
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
```

---

## 📚 Referencias

- **AES-256:** https://en.wikipedia.org/wiki/Advanced_Encryption_Standard
- **Windows Impersonation:** https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-logonuserw
- **SMB/CIFS:** https://en.wikipedia.org/wiki/Server_Message_Block

---

**Última actualización:** 2025-10-28

