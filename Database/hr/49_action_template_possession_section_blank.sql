-- Plantilla ACCION_PERSONAL publicada (TemplateID 20): en la sección POSESIÓN DEL PUESTO, "NRO. ACTA FINAL", "FECHA" y
-- "SERVIDOR PÚBLICO" deben salir vacíos (con o sin la posesión activa) hasta nueva orden.
-- Se renombran a tokens que el backend no llena (POSSESSION_FINAL_RECORD / _DATE / POSSESSION_SERVANT); el motor deja
-- vacío todo token sin valor. Para volver a imprimirlos basta llenar esos tokens en PersonnelActionService.
-- ACTA_NUMBER y APPROVAL_DATE solo se usaban en esta sección; EMPLOYEE_FULLNAME también aparece en otra parte y no se toca.
-- Idempotente.
UPDATE HR.tbl_DocumentTemplates
SET HtmlContent = REPLACE(REPLACE(REPLACE(CAST(HtmlContent AS nvarchar(max)),
        N'NRO. ACTA FINAL: {{ACTA_NUMBER}}',            N'NRO. ACTA FINAL: {{POSSESSION_FINAL_RECORD}}'),
        N'FECHA: {{APPROVAL_DATE}}',                    N'FECHA: {{POSSESSION_FINAL_DATE}}'),
        N'SERVIDOR PÚBLICO: {{EMPLOYEE_FULLNAME}}',     N'SERVIDOR PÚBLICO: {{POSSESSION_SERVANT}}'),
    UpdatedAt = SYSDATETIME()
WHERE TemplateCode = N'ACCION_PERSONAL' AND Status = N'PUBLISHED'
  AND CAST(HtmlContent AS nvarchar(max)) LIKE N'%{{ACTA_NUMBER}}%';
