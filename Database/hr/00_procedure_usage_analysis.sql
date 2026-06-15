-- ============================================================
-- ANÁLISIS DE USO DE PROCEDIMIENTOS ALMACENADOS
-- Fecha: 2026-05-29
-- ============================================================

-- ============================================================
-- ESQUEMA: HR
-- ============================================================

-- [✅ ACTIVOS] Llamados directamente desde C# (HrBackend):
--   sp_hr_AccrueVacationBalance
--   sp_hr_ReserveVacationBalance
--   sp_hr_ReservePermissionBalance
--   sp_hr_ConsumeReservation
--   sp_hr_ReleaseReservation
--   sp_hr_ProcessRecoveryBalance
--   sp_hr_DebitRecoveryBalance
--   sp_hr_GetEmployeeBalances
--   sp_GetEmployeesReport
--   sp_GetAttendanceReport
--   sp_GetDepartmentsReport
--   sp_GetReportAttendanceSumary
--   sp_InsertReportAudit
--   sp_GetReportAudits
--   sp_ProcessAttendanceRunRange
--   sp_ProcessAttendanceRunDate
--   sp_Attendance_CalculateRange
--   sp_Attendance_CalcNightMinutes
--   sp_Justifications_Apply
--   sp_Overtime_Calculate
--   sp_Recovery_Apply
--   sp_Payroll_Discounts
--   sp_Overtime_Price
--   sp_Payroll_Subsidies

-- [🔗 INTERNOS] Llamados por otros SPs (no desde C# directamente):
--   sp_hr_GetVacationParams
--   sp_hr_EnsureTimeBalanceRow
--   sp_ProcessAttendanceForDate
--   sp_ProcessAttendanceBaseDay
--   sp_ProcessAttendanceEmployeeDay
--   sp_ProcessAttendanceFinalizeDay
--   sp_ProcessAttendanceJustificationsDay
--   sp_ProcessAttendanceLeavesDay
--   sp_ProcessAttendancePlanningDay
--   sp_ProcessAttendanceRange
--   sp_ProcessAttendanceRecoveryDay
--   sp_ProcessTimePlanningForEmployeeDay
--   sp_RegisterPersonnelMovement
--   usp_ExecuteScheduleChangePlans

-- [⚠️  CANDIDATOS A DEPURACIÓN] No encontrados en C# ni como helpers:

-- ============================================================
-- ESQUEMA: auth
-- ============================================================

-- [✅ ACTIVOS] Llamados desde RepositoryUta C#:
--   sp_GetMenuByUser

-- [⚠️  CANDIDATOS A DEPURACIÓN] No encontrados en código C#:
--   sp_CleanupOldLogs  --> Puede ser utilitario manual o para administración
--   sp_CreateLocalUser  --> Puede ser utilitario manual o para administración
--   sp_GetApplicationUsageStats  --> Puede ser utilitario manual o para administración
--   sp_GetSecurityReport  --> Puede ser utilitario manual o para administración

-- ============================================================
-- FUNCIONES HR (verificar uso en vistas y SPs)
-- ============================================================
--
-- fn_CalculateNightMinutes  --> usada internamente en SPs de asistencia
-- fn_GetActiveSchedule      --> usada en consultas de horario
-- fn_GetBusinessDays        --> verificar uso
-- fn_hr_CountWorkingDays    --> verificar uso
-- fn_IsHoliday              --> usada en cálculos de asistencia