-- ============================================================================
-- 41_personnel_action_types_hierarchy.sql
-- Jerarquía de tipos de acción de personal (tipo > subtipo), a partir de los prefijos del legado (12.3):
--   1) Subtipos nuevos bajo Nombramiento, Comisión de servicios, Encargo, Traslado, Sanción, Renuncia/Jubilación, Otros y Licencia.
--      Cada subtipo copia las banderas (categoría, vigencia, AD, plantilla, SIIES) de su tipo padre; el de prueba copia las del provisional.
--      Semilla de numeración = mayor número emitido entre HR y el legado en todos los prefijos fusionados (re-sincronizar al corte final).
--   2) "Nombramiento Provisional" (tipo existente) pasa a colgar de "Nombramiento": conserva su id, sus acciones y sus banderas;
--      su contador se lleva a 1202 (último NP del legado) con el formato DTH-UTA-{PREFIX}-{SEQ}-{YEAR} sin reinicio anual.
--   3) Motivos nuevos de Licencia con remuneración (LOSEP art. 27: enfermedad catastrófica o accidente grave, adopción, hijos hospitalizados).
--   4) Plantilla 20: el segundo "REINGRESO" pasa a "REINTEGRO AL CARGO" (campo CB_REINTEGRO).
-- No toca acciones: el agrupamiento de las acciones existentes lo hace apply_types_regroup.py (solo cambia ActionTypeID).
-- Idempotente (se identifica por Code / Name). Reversión: ver el respaldo del script de agrupamiento y restaurar la plantilla 20 desde su respaldo.
-- ============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1) Subtipos --------------------------------------------------------------------
DECLARE @missing NVARCHAR(100) = (
    SELECT TOP 1 v.ParentCode FROM (VALUES (N'NOMBRAMIENTO'),(N'COMISION'),(N'ENCARGO'),(N'TRASLADO'),(N'SANCION_DISCIPLINARIA'),
        (N'RENUNCIA_JUBILACION'),(N'OTROS'),(N'LICENCIA'),(N'Nombramiento Provisional')) v (ParentCode)
    WHERE NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type t WHERE t.Code = v.ParentCode));
IF @missing IS NOT NULL
BEGIN
    DECLARE @msg NVARCHAR(200) = CONCAT(N'Falta el tipo de acción base con Code = ', @missing, N'.');
    THROW 50050, @msg, 1;
END

INSERT INTO HR.tbl_personnel_action_type
    (Name, Code, Description, NumberingPrefix, NumberingYear, NumberingLastSequence, DefaultTemplateId, IsActive, CreatedAt,
     RequiresAdUserCreation, RequiresAdUserDisable, RequiresAdGroupAssignment, ActionCategory, ReachesVigente, SiiesRelacionIesTypeId,
     ParentTypeId, NumberingFormat, NumberingResetsYearly)
SELECT v.Name, v.Code, v.Descr, v.Prefix, YEAR(SYSDATETIME()), v.Seed, f.DefaultTemplateId, 1, SYSDATETIME(),
       f.RequiresAdUserCreation, f.RequiresAdUserDisable, f.RequiresAdGroupAssignment, f.ActionCategory, f.ReachesVigente, f.SiiesRelacionIesTypeId,
       p.PersonnelActionTypeId, N'DTH-UTA-{PREFIX}-{SEQ}-{YEAR}', 0
FROM (VALUES
    -- (padre, banderas desde, nombre, code, descripción, prefijo, semilla)
    (N'NOMBRAMIENTO',          N'NOMBRAMIENTO',             N'Nombramiento permanente',                              N'NOMBRAMIENTO_PERMANENTE',               N'Serie NPR del legado.',                         N'NPR',   420),
    (N'NOMBRAMIENTO',          N'Nombramiento Provisional', N'Nombramiento de prueba',                               N'NOMBRAMIENTO_PRUEBA',                   N'Serie NPP del legado (LOSEP art. 17, b.5).',    N'NPP',   181),
    (N'NOMBRAMIENTO',          N'NOMBRAMIENTO',             N'Nombramiento por elección',                            N'NOMBRAMIENTO_ELECCION',                 N'Serie NPE del legado.',                         N'NPE',    51),
    (N'COMISION',              N'COMISION',                 N'Comisión de servicios con remuneración',               N'COMISION_CON_REMUNERACION',             N'Series CSC y CSA del legado (LOSEP art. 30).',  N'CSC',  1076),
    (N'COMISION',              N'COMISION',                 N'Comisión de servicios sin remuneración',               N'COMISION_SIN_REMUNERACION',             N'Series CSS, CSIF y LCSR del legado (LOSEP art. 31).', N'CSS', 101),
    (N'COMISION',              N'COMISION',                 N'Finalización de comisión de servicios con remuneración', N'FINALIZACION_COMISION_CON_REMUNERACION', N'Series FCSCR y CS(A) del legado.',            N'FCSCR',   2),
    (N'COMISION',              N'COMISION',                 N'Finalización de comisión de servicios sin remuneración', N'FINALIZACION_COMISION_SIN_REMUNERACION', N'Serie FCSSR del legado.',                    N'FCSSR',  40),
    (N'ENCARGO',               N'ENCARGO',                  N'Subrogación',                                          N'SUBROGACION',                           N'Series SUB, SUB A, SBB y SBC del legado.',      N'SUB',   911),
    (N'ENCARGO',               N'ENCARGO',                  N'Encargo del puesto',                                   N'ENCARGO_PUESTO',                        N'Series EP, EPA, EPB, EPC y E del legado.',      N'EP',   1123),
    (N'ENCARGO',               N'ENCARGO',                  N'Encargo funcional',                                    N'ENCARGO_FUNCIONAL',                     N'Series ENCF y EFN del legado.',                 N'ENCF',   61),
    (N'ENCARGO',               N'ENCARGO',                  N'Finalización del encargo',                             N'FINALIZACION_ENCARGO',                  N'Series FNDE y FDEA del legado.',                N'FNDE',   49),
    (N'TRASLADO',              N'TRASLADO',                 N'Traslado administrativo',                              N'TRASLADO_ADMINISTRATIVO',               N'Serie TRAA del legado (LOSEP art. 35).',        N'TRAA',   41),
    (N'TRASLADO',              N'TRASLADO',                 N'Traspaso de puesto docente',                           N'TRASPASO_PUESTO_DOCENTE',               N'Series TPD, TPDA y TRLD del legado (LOSEP art. 37).', N'TPD', 35),
    (N'TRASLADO',              N'TRASLADO',                 N'Traspaso de puesto administrativo',                    N'TRASPASO_PUESTO_ADMINISTRATIVO',        N'Serie TPA del legado (LOSEP art. 37).',         N'TPA',    55),
    (N'SANCION_DISCIPLINARIA', N'SANCION_DISCIPLINARIA',    N'Sanción disciplinaria (general)',                      N'SANCION_GENERAL',                       N'Series SD y SD(A) del legado.',                 N'SD',    420),
    (N'SANCION_DISCIPLINARIA', N'SANCION_DISCIPLINARIA',    N'Sanción: separación definitiva',                       N'SANCION_SEPARACION_DEFINITIVA',         N'Serie INVAL del legado.',                       N'INVAL',  29),
    (N'SANCION_DISCIPLINARIA', N'SANCION_DISCIPLINARIA',    N'Sanción: multa 10%',                                   N'SANCION_MULTA_10',                      N'Serie INVA del legado.',                        N'INVA',   38),
    (N'SANCION_DISCIPLINARIA', N'SANCION_DISCIPLINARIA',    N'Sanción: amonestación escrita',                        N'SANCION_AMONESTACION_ESCRITA',          N'Serie AMOES del legado.',                       N'AMOES',   7),
    (N'RENUNCIA_JUBILACION',   N'RENUNCIA_JUBILACION',      N'Renuncia',                                             N'RENUNCIA',                              N'Serie RE del legado.',                          N'RE',    812),
    (N'RENUNCIA_JUBILACION',   N'RENUNCIA_JUBILACION',      N'Insubsistencia',                                       N'INSUBSISTENCIA',                        N'Series ISUB, ISUBB, INA e INSC del legado.',    N'ISUB',   36),
    (N'OTROS',                 N'OTROS',                    N'Rotación de personal',                                 N'ROTACION_PERSONAL',                     N'Serie RDP del legado.',                         N'RDP',   196),
    (N'OTROS',                 N'OTROS',                    N'Cambio de denominación',                               N'CAMBIO_DENOMINACION',                   N'Nuevo; sin histórico en el legado.',            N'CDEN',    0),
    (N'OTROS',                 N'OTROS',                    N'Otros (general)',                                      N'OTROS_GENERAL',                         N'Series OTR, OT1, OB y OC del legado (incluye finalización de funciones).', N'OTR', 377),
    (N'LICENCIA',              N'LICENCIA',                 N'Permiso',                                              N'PERMISO',                               N'Series PER y PRA del legado (LOSEP art. 33).',  N'PER',   115),
    (N'LICENCIA',              N'LICENCIA',                 N'Finalización de permiso otorgado',                     N'FINALIZACION_PERMISO',                  N'Nuevo; sin histórico en el legado.',            N'FPO',     0)
) v (ParentCode, FlagsCode, Name, Code, Descr, Prefix, Seed)
JOIN HR.tbl_personnel_action_type p ON p.Code = v.ParentCode
JOIN HR.tbl_personnel_action_type f ON f.Code = v.FlagsCode
WHERE NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type x WHERE x.Code = v.Code);
GO

-- 2) "Nombramiento Provisional" pasa a ser subtipo de "Nombramiento" ----------------
UPDATE c
   SET c.ParentTypeId = p.PersonnelActionTypeId,
       c.NumberingFormat = N'DTH-UTA-{PREFIX}-{SEQ}-{YEAR}',
       c.NumberingResetsYearly = 0,
       c.NumberingLastSequence = CASE WHEN c.NumberingLastSequence < 1202 THEN 1202 ELSE c.NumberingLastSequence END,
       c.UpdatedAt = SYSDATETIME()
  FROM HR.tbl_personnel_action_type c
  JOIN HR.tbl_personnel_action_type p ON p.Code = N'NOMBRAMIENTO'
 WHERE c.Code = N'Nombramiento Provisional' AND c.ParentTypeId IS NULL;
GO

-- 3) Motivos nuevos de Licencia con remuneración -------------------------------------
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, CreatedAt, SortOrder, Metadata)
SELECT N'PERSONNEL_ACTION_LEAVE_REASON', v.Name, v.Descr, 1, SYSDATETIME(), v.SortOrder, v.Meta
FROM (VALUES
    (N'Enfermedad catastrófica o accidente grave',           N'Licencia con remuneración (LOSEP art. 27, literal b); hasta seis meses, certificada.', 60, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Adopción',                                            N'Licencia con remuneración (LOSEP art. 27, literal g); treinta días.',                 70, N'{"types":["LCR"],"remuneration":"WITH"}'),
    (N'Hijos hospitalizados o con patologías degenerativas', N'Licencia con remuneración (LOSEP art. 27, literal h); veinte y cinco días.',          80, N'{"types":["LCR"],"remuneration":"WITH"}')
) v (Name, Descr, SortOrder, Meta)
WHERE NOT EXISTS (SELECT 1 FROM HR.ref_Types r WHERE r.Category = N'PERSONNEL_ACTION_LEAVE_REASON' AND r.Name = v.Name);
GO

-- 4) Plantilla 20: segundo "REINGRESO" -> "REINTEGRO AL CARGO" ---------------------------
DECLARE @old NVARCHAR(MAX) =
    N'<span class="action-clean-option"><span class="cb {{CB_REINGRESO2}}">{{CB_REINGRESO2}}</span><span class="lbl {{CB_REINGRESO2}}">REINGRESO</span></span>';
DECLARE @new NVARCHAR(MAX) =
    N'<span class="action-clean-option"><span class="cb {{CB_REINTEGRO}}">{{CB_REINTEGRO}}</span><span class="lbl {{CB_REINTEGRO}}">REINTEGRO AL CARGO</span></span>';
DECLARE @h NVARCHAR(MAX) = (SELECT HtmlContent FROM HR.tbl_DocumentTemplates WHERE TemplateID = 20);

IF @h IS NOT NULL AND CHARINDEX(N'CB_REINGRESO2', @h) > 0
BEGIN
    IF CHARINDEX(@old, @h) = 0 THROW 50051, 'No se encontró el bloque REINGRESO (2da fila) esperado en la plantilla 20.', 1;
    UPDATE HR.tbl_DocumentTemplates
       SET HtmlContent = REPLACE(@h, @old, @new),
           Version = CAST(ISNULL(TRY_CAST(Version AS INT), 1) + 1 AS NVARCHAR(20)),
           UpdatedAt = SYSDATETIME()
     WHERE TemplateID = 20;
END
GO

UPDATE HR.tbl_DocumentTemplateFields
   SET FieldName = N'CB_REINTEGRO', Label = N'Checkbox: Reintegro al cargo'
 WHERE TemplateID = 20 AND FieldName = N'CB_REINGRESO2'
   AND NOT EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplateFields f WHERE f.TemplateID = 20 AND f.FieldName = N'CB_REINTEGRO');
GO
