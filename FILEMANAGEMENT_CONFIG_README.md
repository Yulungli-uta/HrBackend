# FileManagement - Configuración de Credenciales Encriptadas

## 📋 Descripción General

El servicio **FileManagement** permite subir, descargar y eliminar archivos en directorios de red (NAS/SMB) usando **credenciales encriptadas** almacenadas en `appsettings.json`. Las credenciales se encriptan con **AES-256** y se usan mediante **Windows Impersonation** para acceder a recursos de red protegidos.

---

## 🔐 Características de Seguridad

- ✅ **Encriptación AES-256** para credenciales
- ✅ **Windows Impersonation** para acceso a NAS/SMB
- ✅ **Credenciales centralizadas** en appsettings.json
- ✅ **Clave de encriptación separada** del código
- ✅ **Prevención de path traversal**
- ✅ **Validación de extensiones y tamaños**

---

## 🚀 Configuración Paso a Paso

### **Paso 1: Generar Clave de Encriptación**

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

### **Paso 2: Encriptar Credenciales**

Usar la herramienta `CredentialEncryptor` para encriptar las credenciales del NAS.

#### **Ejecutar la herramienta:**

```bash
cd Tools
dotnet run -- --key "MySecretKey123456789012345678" --username "nas_user" --password "nas_password123" --domain "WORKGROUP"
```

#### **Salida esperada:**

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

### **Paso 3: Agregar Configuración a appsettings.json**

Copiar la salida del paso anterior en `appsettings.json`:

```json
{
  "FileManagement": {
    "EncryptionKey": "MySecretKey123456789012345678",
    "NetworkCredentials": {
      "Username": "dGVzdF9lbmNyeXB0ZWRfdXNlcm5hbWU=",
      "Password": "dGVzdF9lbmNyeXB0ZWRfcGFzc3dvcmQ=",
      "Domain": "dGVzdF9lbmNyeXB0ZWRfZG9tYWlu"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**Nota:** El campo `Domain` es opcional. Si no usas dominio, puedes omitirlo o dejarlo vacío.

---

### **Paso 4: Registrar Servicios en Program.cs**

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

### **Paso 5: Configurar DirectoryParameters en la Base de Datos**

Insertar un registro en la tabla `TBL_DirectoryParameters`:

```sql
INSERT INTO TBL_DirectoryParameters 
(Code, PhysicalPath, RelativePath, Description, Extension, MaxSizeMB, Status, CreatedAt, CreatedBy)
VALUES 
('NAS_CONTRACTS', 
 '\\192.168.1.100\shared\contracts', 
 '/contracts', 
 'Directorio de contratos en NAS', 
 '.pdf,.docx,.xlsx', 
 10, 
 1, 
 GETDATE(), 
 1);
```

**Campos importantes:**
- `Code`: Identificador único para usar en la API
- `PhysicalPath`: Ruta UNC del NAS (ej: `\\server\share` o `D:\Files`)
- `Extension`: Extensiones permitidas separadas por comas
- `MaxSizeMB`: Tamaño máximo en MB

---

## 📡 Endpoints API

### **1. Subir Archivo**

```http
POST /files/upload
Content-Type: multipart/form-data

FormData:
  - DirectoryCode: "NAS_CONTRACTS"
  - RelativePath: "/contracts/"
  - FileName: "contrato_001.pdf"
  - File: [archivo binario]
```

**Respuesta:**
```json
{
  "success": true,
  "message": "File uploaded successfully.",
  "fullPath": "\\\\192.168.1.100\\shared\\contracts\\contracts\\2025\\contrato_001.pdf",
  "relativePath": "/contracts/2025/contrato_001.pdf",
  "fileName": "contrato_001.pdf",
  "fileSize": 524288,
  "year": 2025
}
```

---

### **2. Subir Múltiples Archivos**

```http
POST /files/upload-multiple
Content-Type: multipart/form-data

FormData:
  - DirectoryCode: "NAS_CONTRACTS"
  - RelativePath: "/contracts/"
  - Files: [archivo1.pdf, archivo2.pdf, archivo3.pdf]
```

**Respuesta:**
```json
[
  {
    "success": true,
    "message": "File uploaded successfully.",
    "fullPath": "\\\\192.168.1.100\\shared\\contracts\\contracts\\2025\\archivo1.pdf",
    "relativePath": "/contracts/2025/archivo1.pdf",
    "fileName": "archivo1.pdf",
    "fileSize": 524288,
    "year": 2025
  },
  {
    "success": true,
    "message": "File uploaded successfully.",
    "fullPath": "\\\\192.168.1.100\\shared\\contracts\\contracts\\2025\\archivo2.pdf",
    "relativePath": "/contracts/2025/archivo2.pdf",
    "fileName": "archivo2.pdf",
    "fileSize": 612352,
    "year": 2025
  }
]
```

---

### **3. Descargar Archivo**

```http
GET /files/download/NAS_CONTRACTS?filePath=/contracts/2025/contrato_001.pdf
```

**Respuesta:**
- Archivo binario con Content-Type correcto

---

### **4. Eliminar Archivo**

```http
DELETE /files/delete/NAS_CONTRACTS?filePath=/contracts/2025/contrato_001.pdf
```

**Respuesta:**
```json
{
  "success": true,
  "message": "File deleted successfully.",
  "filePath": "/contracts/2025/contrato_001.pdf"
}
```

---

### **5. Verificar si Existe Archivo**

```http
GET /files/exists/NAS_CONTRACTS?filePath=/contracts/2025/contrato_001.pdf
```

**Respuesta:**
```json
{
  "exists": true
}
```

---

## 🔧 Funcionamiento Interno

### **Flujo de Upload con Credenciales:**

1. **Cliente** envía archivo + directoryCode + relativePath
2. **Controller** recibe el request
3. **Service** busca DirectoryParameters por código
4. **Service** valida extensión y tamaño
5. **Service** desencripta credenciales de appsettings
6. **Service** inicia Windows Impersonation con credenciales del NAS
7. **Service** crea carpeta del año si no existe (ej: `/contracts/2025/`)
8. **Service** guarda el archivo en el NAS
9. **Service** finaliza Impersonation
10. **Service** retorna resultado con fullPath

---

## 🛡️ Seguridad

### **Credenciales Encriptadas:**
- Las credenciales **NUNCA** se almacenan en texto plano
- Se encriptan con AES-256 antes de guardarlas en appsettings
- La clave de encriptación debe mantenerse **secreta**

### **Windows Impersonation:**
- Las operaciones de archivos se ejecutan con las credenciales del NAS
- El impersonation se inicia **solo durante la operación** y se finaliza inmediatamente
- Si falla la autenticación, la operación se cancela

### **Validaciones:**
- ✅ Extensión de archivo contra lista permitida
- ✅ Tamaño de archivo contra límite configurado
- ✅ Path traversal (sanitización de rutas)
- ✅ Existencia de directorio configurado

---

## 🐛 Solución de Problemas

### **Error: "LogonUser failed with error code: 1326"**
- **Causa:** Usuario o contraseña incorrectos
- **Solución:** Verificar credenciales y volver a encriptar

### **Error: "Directory with code 'XXX' not found"**
- **Causa:** No existe el registro en `TBL_DirectoryParameters`
- **Solución:** Insertar el registro con el código correcto

### **Error: "File extension '.xyz' is not allowed"**
- **Causa:** La extensión no está en la lista permitida
- **Solución:** Agregar la extensión en el campo `Extension` de DirectoryParameters

### **Error: "File size exceeds maximum allowed size"**
- **Causa:** El archivo es más grande que `MaxSizeMB`
- **Solución:** Aumentar `MaxSizeMB` o reducir el tamaño del archivo

### **Error: "PlatformNotSupportedException"**
- **Causa:** Windows Impersonation solo funciona en Windows
- **Solución:** Ejecutar en Windows Server o usar alternativas para Linux

---

## 📝 Ejemplo Completo

### **1. Encriptar credenciales:**
```bash
dotnet run --project Tools/CredentialEncryptor.cs -- --key "MyKey12345678901234567890123" --username "admin" --password "P@ssw0rd" --domain "CORP"
```

### **2. Configurar appsettings.json:**
```json
{
  "FileManagement": {
    "EncryptionKey": "MyKey12345678901234567890123",
    "NetworkCredentials": {
      "Username": "encrypted_username_here",
      "Password": "encrypted_password_here",
      "Domain": "encrypted_domain_here"
    }
  }
}
```

### **3. Insertar DirectoryParameters:**
```sql
INSERT INTO TBL_DirectoryParameters VALUES 
('DOCS', '\\\\nas\documents', '/docs', 'Documentos', '.pdf,.docx', 5, 1, GETDATE(), 1, NULL, NULL);
```

### **4. Subir archivo desde Postman:**
```
POST http://localhost:5000/files/upload
Body: form-data
  - DirectoryCode: DOCS
  - RelativePath: /docs/
  - FileName: documento.pdf
  - File: [seleccionar archivo]
```

---

## 📚 Referencias

- **AES-256:** https://en.wikipedia.org/wiki/Advanced_Encryption_Standard
- **Windows Impersonation:** https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-logonuserw
- **SMB/CIFS:** https://en.wikipedia.org/wiki/Server_Message_Block

---

**Última actualización:** 2025-10-22

