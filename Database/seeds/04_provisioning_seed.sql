-- ============================================================
-- SEED: Datos iniciales de Provisioning
-- Fuente: migration_phase1_provisioning_seed.sql
-- Generado: 2026-05-29
-- ============================================================

-- =============================================================================
-- Seed Fase 1: ProvisioningStatus en HR.ref_Types
-- Base de datos: dbutasystem (schema: HR)
-- DescripciÃ³n:
--   Inserta los estados de aprovisionamiento en HR.ref_Types con TypeIDs
--   fijos 2001-2007 usando SET IDENTITY_INSERT. Estos IDs coinciden con el
--   enum ProvisioningStatus definido en RepositoryUta, permitiendo JOINs
--   desde reportes HR:
--
--     tbl_UserProvisioning.ProvisioningStatusId â†’ HR.ref_Types.TypeId
--     tbl_UserProvisioning.EmployeeTypeId       â†’ HR.ref_Types.TypeId
--                                                  (1=Docente, 2=Admin â€” ya existen)
--
-- Ejecutar ANTES de: migration_phase1_provisioning.sql (auth DB)
-- =============================================================================

USE dbutasystem;   -- ajustar al nombre real de la BD HR
GO

-- Verificar que los TypeIDs 2001-2007 no estÃ©n ocupados por otro registro
IF EXISTS (
    SELECT 1 FROM HR.ref_Types
    WHERE TypeID BETWEEN 2001 AND 2007
      AND Category <> 'PROVISIONING_STATUS'
)
BEGIN
    RAISERROR (
        'ERROR: Los TypeIDs 2001-2007 ya estÃ¡n ocupados por otra categorÃ­a. Revisar ref_Types antes de continuar.',
        16, 1
    );
    RETURN;
END
GO

-- Insertar estados de aprovisionamiento con IDs fijos
SET IDENTITY_INSERT HR.ref_Types ON;
GO

IF NOT EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = 'PROVISIONING_STATUS')
BEGIN
    INSERT INTO HR.ref_Types (TypeID, Category, Name, Description, IsActive)
    VALUES
        (2001, 'PROVISIONING_STATUS', 'Requested',
         'Aprovisionamiento solicitado, pendiente de ejecuciÃ³n', 1),

        (2002, 'PROVISIONING_STATUS', 'CreatedInLocalAd',
         'Usuario creado en Active Directory local; pendiente de sincronizaciÃ³n con Entra', 1),

        (2003, 'PROVISIONING_STATUS', 'PendingEntraSync',
         'Esperando sincronizaciÃ³n de Entra Connect (puede tardar hasta 30 min)', 1),

        (2004, 'PROVISIONING_STATUS', 'SyncedInEntra',
         'Usuario sincronizado y activo en Microsoft Entra ID', 1),

        (2005, 'PROVISIONING_STATUS', 'LicenseAssigned',
         'Licencia Office 365 asignada exitosamente', 1),

        (2006, 'PROVISIONING_STATUS', 'LicenseFailed',
         'Error al asignar licencia Office 365; revisar disponibilidad de SKUs', 1),

        (2007, 'PROVISIONING_STATUS', 'LocalAdFailed',
         'Error al crear usuario en Active Directory local', 1);

    PRINT 'HR.ref_Types: estados PROVISIONING_STATUS (2001-2007) insertados';
END
ELSE
    PRINT 'HR.ref_Types: PROVISIONING_STATUS ya existe, omitiendo';
GO

SET IDENTITY_INSERT HR.ref_Types OFF;
GO

-- =============================================================================
-- VerificaciÃ³n: mostrar los registros insertados
-- =============================================================================

SELECT TypeID, Category, Name, Description
FROM   HR.ref_Types
WHERE  Category IN ('PROVISIONING_STATUS', 'CONTRACT_TYPE')
ORDER  BY Category, TypeID;
GO

-- =============================================================================
-- Nota sobre CONTRACT_TYPE (ya existentes en ref_Types):
--   57 = LOSEP
--   58 = LOES
--   59 = CÃ³digo Trabajo
-- El campo EmployeeType en tbl_Employees referencia estos TypeIDs.
-- Todos los empleados usan la misma licencia O365 (AppParam "lic:employee").
-- =============================================================================

PRINT '=== Seed HR DB Phase 1 completado ===';
GO
