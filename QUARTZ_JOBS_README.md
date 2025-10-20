# Quartz.NET Jobs - Sistema de Asistencia y Nómina

## 📋 Descripción General

Este proyecto utiliza **Quartz.NET** para ejecutar automáticamente procedimientos almacenados de asistencia y nómina en horarios programados. Los jobs se ejecutan en segundo plano sin intervención manual.

---

## 🚀 Configuración Inicial

### 1. Instalar Paquetes NuGet

Ejecutar los siguientes comandos en la raíz del proyecto:

```bash
dotnet add package Quartz
dotnet add package Quartz.Extensions.Hosting
dotnet add package Quartz.Extensions.DependencyInjection
```

### 2. Registrar Quartz en Program.cs

Agregar la siguiente línea en `Program.cs`:

```csharp
using WsUtaSystem.Infrastructure.DependencyInjection;

// ... otras configuraciones ...

// Agregar Quartz.NET Jobs
builder.Services.AddQuartzJobs();
```

### 3. Verificar Zona Horaria

La zona horaria por defecto es `America/Guayaquil`. Para cambiarla, editar el archivo:
`Infrastructure/DependencyInjection/QuartzConfiguration.cs`

```csharp
const string timeZone = "America/Guayaquil"; // Cambiar aquí
```

---

## 📅 Jobs Configurados

### **Jobs Diarios (Asistencia)**

| Job | Horario | Descripción | Procedimiento |
|-----|---------|-------------|---------------|
| **DailyAttendanceCalculationJob** | 2:00 AM | Calcula asistencia del día anterior | `HR.sp_Attendance_CalculateRange` |
| **DailyNightMinutesCalculationJob** | 3:00 AM | Calcula minutos nocturnos del día anterior | `HR.sp_Attendance_CalcNightMinutes` |
| **DailyJustificationsJob** | 4:00 AM | Aplica justificaciones aprobadas | `HR.sp_Justifications_Apply` |
| **DailyRecoveryJob** | 5:00 AM | Aplica recuperaciones de tiempo | `HR.sp_Recovery_Apply` |

---

### **Jobs Mensuales (Nómina)**

| Job | Horario | Descripción | Procedimiento |
|-----|---------|-------------|---------------|
| **MonthlyOvertimePriceJob** | Día 1 - 2:00 AM | Calcula precio de horas extra del mes anterior | `HR.sp_Overtime_Price` |
| **MonthlyPayrollDiscountsJob** | Día 1 - 3:00 AM | Calcula descuentos por atrasos/ausencias | `HR.sp_Payroll_Discounts` |
| **MonthlyPayrollSubsidiesJob** | Día 1 - 4:00 AM | Calcula subsidios y recargos nocturnos/feriados | `HR.sp_Payroll_Subsidies` |

---

## 🔧 Formato de Cron Expressions

Quartz.NET utiliza **cron expressions de 6 campos**:

```
┌─────────── segundos (0-59)
│ ┌───────── minutos (0-59)
│ │ ┌─────── horas (0-23)
│ │ │ ┌───── día del mes (1-31)
│ │ │ │ ┌─── mes (1-12)
│ │ │ │ │ ┌─ día de la semana (0-6, 0=Domingo)
│ │ │ │ │ │
* * * * * *
```

### Ejemplos:

- `0 0 2 * * ?` → Todos los días a las 2:00 AM
- `0 0 3 1 * ?` → Día 1 de cada mes a las 3:00 AM
- `0 0 9-17 * * 1-5` → Lunes a Viernes, cada hora de 9 AM a 5 PM
- `0 */15 * * * *` → Cada 15 minutos

---

## 🛠️ Personalización de Horarios

Para cambiar los horarios de ejecución, editar el archivo:
`Infrastructure/DependencyInjection/QuartzConfiguration.cs`

**Ejemplo: Cambiar el horario del cálculo de asistencia a las 6:00 AM:**

```csharp
q.AddTrigger(opts => opts
    .ForJob(dailyAttendanceKey)
    .WithIdentity("DailyAttendanceCalculationTrigger")
    .WithCronSchedule("0 0 6 * * ?", x => x  // Cambiar de 2 a 6
        .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone)))
    .WithDescription("Ejecuta el cálculo de asistencia diariamente a las 6:00 AM")
    .UsingJobData("TimeZone", timeZone));
```

---

## 📊 Monitoreo y Logs

Los jobs registran información en los logs de la aplicación usando `ILogger`:

```csharp
_logger.LogInformation("Starting daily attendance calculation for date: {Date}", yesterday);
_logger.LogError(ex, "Error executing daily attendance calculation job");
```

Para ver los logs, configurar el nivel de logging en `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "WsUtaSystem.Infrastructure.Jobs": "Debug"
    }
  }
}
```

---

## 🔄 Ejecución Manual de Jobs

Aunque los jobs se ejecutan automáticamente, también puedes ejecutar los procedimientos manualmente mediante los endpoints API:

### **Asistencia:**
```http
POST /cv/attendance/calculate-range
Content-Type: application/json

{
  "fromDate": "2025-01-01",
  "toDate": "2025-01-31",
  "employeeId": null
}
```

### **Nómina:**
```http
POST /cv/overtime/price
Content-Type: application/json

{
  "period": "2025-01"
}
```

---

## ⚠️ Consideraciones Importantes

### **1. DisallowConcurrentExecution**
Todos los jobs tienen el atributo `[DisallowConcurrentExecution]` para evitar que se ejecuten múltiples instancias al mismo tiempo.

### **2. Orden de Ejecución Diaria**
Los jobs diarios se ejecutan en secuencia con 1 hora de diferencia para asegurar que los datos estén disponibles:

1. **2:00 AM** - Cálculo de asistencia
2. **3:00 AM** - Minutos nocturnos (requiere datos de asistencia)
3. **4:00 AM** - Justificaciones (requiere datos de asistencia)
4. **5:00 AM** - Recuperaciones (requiere datos de asistencia)

### **3. Zona Horaria**
Todos los jobs usan la zona horaria configurada (`America/Guayaquil` por defecto). Los cálculos de "día anterior" se basan en esta zona horaria.

### **4. Manejo de Errores**
Si un job falla, Quartz.NET registrará el error en los logs pero **NO detendrá** los demás jobs. Cada job es independiente.

---

## 🧪 Pruebas

### **Verificar que Quartz está funcionando:**

1. Ejecutar la aplicación
2. Revisar los logs al inicio, deberías ver:
   ```
   Quartz Scheduler v3.x.x created.
   Scheduler started.
   ```

### **Probar un job manualmente (para desarrollo):**

Puedes crear un endpoint temporal para ejecutar un job inmediatamente:

```csharp
[HttpPost("test/run-attendance-job")]
public async Task<IActionResult> TestAttendanceJob(
    [FromServices] ISchedulerFactory schedulerFactory)
{
    var scheduler = await schedulerFactory.GetScheduler();
    var jobKey = new JobKey("DailyAttendanceCalculationJob");
    await scheduler.TriggerJob(jobKey);
    return Ok("Job triggered");
}
```

---

## 📚 Recursos Adicionales

- **Documentación oficial de Quartz.NET:** https://www.quartz-scheduler.net/
- **Cron Expression Generator:** https://www.freeformatter.com/cron-expression-generator-quartz.html
- **Zonas horarias .NET:** https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo

---

## 🐛 Solución de Problemas

### **Los jobs no se ejecutan:**
1. Verificar que `AddQuartzJobs()` está llamado en `Program.cs`
2. Revisar logs para errores de Quartz
3. Verificar que los paquetes NuGet están instalados

### **Jobs se ejecutan en horario incorrecto:**
1. Verificar la zona horaria configurada
2. Revisar las cron expressions
3. Verificar la hora del servidor

### **Error de inyección de dependencias:**
1. Asegurarse de que todos los servicios (IAttendanceCalculationService, etc.) están registrados en DI
2. Verificar que `UseMicrosoftDependencyInjectionJobFactory()` está configurado

---

## 📝 Notas de Desarrollo

- Los jobs están en `Infrastructure/Jobs/`
- La configuración está en `Infrastructure/DependencyInjection/QuartzConfiguration.cs`
- Cada job hereda de `BaseJob` para funcionalidad común
- Los jobs usan los mismos servicios que los controladores API

---

**Última actualización:** 2025-10-20

