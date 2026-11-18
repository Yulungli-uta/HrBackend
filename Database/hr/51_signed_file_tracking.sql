-- ============================================================================
-- 51_signed_file_tracking.sql
-- Pantallas "Acciones con archivo firmado" y "Contratos con archivo firmado" (solo lectura):
--   1) HR.vw_PersonnelActionSignedFile: acciones de personal activas con enlace a un archivo firmado activo.
--   2) HR.vw_ContractSignedFile:        contratos activos con enlace a un archivo firmado activo.
--   3) Opciones de menú bajo "Acción Personal" y "Gestion de Contratos" (copian los roles de Histórico Acciones / Histórico Contratos).
-- Archivo "activo" = HR.TBL_StoredFile con Status = 1 y no borrado (DeletedAt NULL o 0001-01-01).
-- Las vistas solo listan los que SÍ tienen enlace; no existe vista de faltantes.
-- Los textos de estado salen por nombre (ref_Types.Name), no por TypeId. Idempotente.
-- Esquema HR (vistas) y auth (menú, compartido con RepositoryUta).
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW HR.vw_PersonnelActionSignedFile
AS
SELECT
    a.ActionId,
    a.ActionNumber,
    a.PersonId,
    p.IDCard                                   AS PersonIdCard,
    LTRIM(RTRIM(CONCAT(p.LastName, N' ', p.FirstName))) AS PersonName,
    COALESCE(dd.DepartmentID, od.DepartmentID) AS DepartmentId,
    COALESCE(dd.Name, od.Name)                 AS DepartmentName,
    a.ActionTypeId,
    CASE WHEN pt.Name IS NOT NULL THEN CONCAT(pt.Name, N' > ', t.Name) ELSE t.Name END AS ActionTypeName,
    a.Status,
    a.EffectiveDate,
    a.EndDate,
    a.ElaboratorID                             AS ElaboratorEmployeeId,
    ep.IDCard                                  AS ElaboratorIdCard,
    LTRIM(RTRIM(CONCAT(ep.LastName, N' ', ep.FirstName))) AS ElaboratorName,
    sf.FileId                                  AS SignedFileId,
    sf.FileGuid                                AS SignedFileGuid,
    sf.OriginalFileName                        AS SignedFileName,
    sf.SizeBytes                               AS SignedFileSizeBytes,
    sf.CreatedAt                               AS SignedFileUploadedAt
FROM HR.tbl_PersonnelActions a
JOIN HR.TBL_StoredFile sf
  ON sf.FileId = a.SignedDocumentStoredFileId
 AND sf.Status = 1
 AND (sf.DeletedAt IS NULL OR sf.DeletedAt < '1900-01-01')
JOIN HR.tbl_personnel_action_type t ON t.PersonnelActionTypeId = a.ActionTypeId
LEFT JOIN HR.tbl_personnel_action_type pt ON pt.PersonnelActionTypeId = t.ParentTypeId
LEFT JOIN HR.tbl_People p ON p.PersonID = a.PersonId
LEFT JOIN HR.tbl_Departments dd ON dd.DepartmentID = a.DestinationDepartmentId
LEFT JOIN HR.tbl_Departments od ON od.DepartmentID = a.OriginDepartmentId
LEFT JOIN HR.tbl_Employees ee ON ee.EmployeeID = a.ElaboratorID
LEFT JOIN HR.tbl_People ep ON ep.PersonID = ee.PersonID
WHERE a.IsDeleted = 0;
GO

CREATE OR ALTER VIEW HR.vw_ContractSignedFile
AS
SELECT
    c.ContractID,
    c.ContractCode,
    c.PersonID                                 AS PersonId,
    p.IDCard                                   AS PersonIdCard,
    LTRIM(RTRIM(CONCAT(p.LastName, N' ', p.FirstName))) AS PersonName,
    c.DepartmentID                             AS DepartmentId,
    d.Name                                     AS DepartmentName,
    c.ContractTypeID                           AS ContractTypeId,
    ct.name                                    AS ContractTypeName,
    c.Status                                   AS StatusTypeId,
    rt.Name                                    AS StatusName,
    c.StartDate,
    c.EndDate,
    c.CreatedBy                                AS CreatedByEmployeeId,
    cp.IDCard                                  AS CreatedByIdCard,
    LTRIM(RTRIM(CONCAT(cp.LastName, N' ', cp.FirstName))) AS CreatedByName,
    sf.FileId                                  AS SignedFileId,
    sf.FileGuid                                AS SignedFileGuid,
    sf.OriginalFileName                        AS SignedFileName,
    sf.SizeBytes                               AS SignedFileSizeBytes,
    sf.CreatedAt                               AS SignedFileUploadedAt
FROM HR.tbl_Contracts c
JOIN HR.TBL_StoredFile sf
  ON sf.FileId = c.SignedDocumentStoredFileId
 AND sf.Status = 1
 AND (sf.DeletedAt IS NULL OR sf.DeletedAt < '1900-01-01')
LEFT JOIN HR.tbl_People p ON p.PersonID = c.PersonID
LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = c.DepartmentID
LEFT JOIN HR.tbl_contract_type ct ON ct.ContractTypeID = c.ContractTypeID
LEFT JOIN HR.ref_Types rt ON rt.TypeId = c.Status
LEFT JOIN HR.tbl_Employees ce ON ce.EmployeeID = c.CreatedBy
LEFT JOIN HR.tbl_People cp ON cp.PersonID = ce.PersonID
WHERE c.IsDeleted = 0;
GO

-- 3) Menú ---------------------------------------------------------------------------
DECLARE @items TABLE (SourceUrl NVARCHAR(200), NewName NVARCHAR(100), NewUrl NVARCHAR(200));
INSERT INTO @items VALUES
    (N'/personnel-actions/historical', N'Acciones con archivo firmado',   N'/personnel-actions/signed-files'),
    (N'/contracts/historical',         N'Contratos con archivo firmado', N'/contracts/signed-files');

DECLARE @SourceUrl NVARCHAR(200), @NewName NVARCHAR(100), @NewUrl NVARCHAR(200), @SourceId INT, @MenuId INT;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT SourceUrl, NewName, NewUrl FROM @items;
OPEN cur;
FETCH NEXT FROM cur INTO @SourceUrl, @NewName, @NewUrl;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @SourceId = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Url = @SourceUrl AND IsDeleted = 0);
    IF @SourceId IS NULL
        THROW 50070, 'No se encontró el menú de referencia (Histórico Acciones / Histórico Contratos).', 1;

    SELECT @MenuId = (SELECT TOP 1 Id FROM auth.tbl_MenuItems WHERE Url = @NewUrl AND IsDeleted = 0);
    IF @MenuId IS NULL
    BEGIN
        INSERT INTO auth.tbl_MenuItems (Name, Url, Icon, ParentId, [Order], IsVisible, ModuleName, IsDeleted)
        SELECT @NewName, @NewUrl, N'FileCheck', s.ParentId,
               (SELECT ISNULL(MAX([Order]), 0) + 1 FROM auth.tbl_MenuItems WHERE ParentId = s.ParentId AND IsDeleted = 0),
               1, s.ModuleName, 0
        FROM auth.tbl_MenuItems s WHERE s.Id = @SourceId;
        SET @MenuId = SCOPE_IDENTITY();
    END

    INSERT INTO auth.tbl_RoleMenuItems (RoleId, MenuItemId, IsVisible)
    SELECT rm.RoleId, @MenuId, rm.IsVisible
    FROM auth.tbl_RoleMenuItems rm
    WHERE rm.MenuItemId = @SourceId
      AND NOT EXISTS (SELECT 1 FROM auth.tbl_RoleMenuItems x WHERE x.RoleId = rm.RoleId AND x.MenuItemId = @MenuId);

    FETCH NEXT FROM cur INTO @SourceUrl, @NewName, @NewUrl;
END
CLOSE cur; DEALLOCATE cur;
GO

-- Verificación (esperado: 2668 acciones y 8507 contratos con archivo activo) ------------
SELECT N'acciones' AS Vista, COUNT(*) AS Filas FROM HR.vw_PersonnelActionSignedFile
UNION ALL SELECT N'contratos', COUNT(*) FROM HR.vw_ContractSignedFile;
SELECT Id, Name, Url, ParentId FROM auth.tbl_MenuItems WHERE Url IN (N'/personnel-actions/signed-files', N'/contracts/signed-files');
