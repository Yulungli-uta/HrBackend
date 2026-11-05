/* ============================================================================
   32_researcher_profile_orcid_optional.sql
   Permite guardar el ID de una plataforma (p. ej. Scopus Author ID) de un investigador que
   todavía no tiene ORCID registrado. La sincronización de publicaciones sigue exigiendo ORCID.

   Cambios en HR.tbl_ResearcherProfiles (solo esta tabla):
     1. Orcid pasa a admitir NULL (el CHECK de formato no cambia; un NULL no lo viola).
     2. OrcidEnteredAt pasa a admitir NULL (sin ORCID no hay fecha de ingreso).
     3. El índice único del ORCID pasa a ser filtrado (WHERE Orcid IS NOT NULL): con el índice
        único normal solo cabría un perfil sin ORCID.

   Idempotente. Ejecutar con sqlcmd -f 65001.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID('HR.tbl_ResearcherProfiles', 'U') IS NULL
    THROW 50000, 'Falta HR.tbl_ResearcherProfiles: aplicar antes 31_scientific_publications_sync.sql.', 1;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HR.tbl_ResearcherProfiles') AND name = 'Orcid' AND is_nullable = 0)
    ALTER TABLE HR.tbl_ResearcherProfiles ALTER COLUMN Orcid VARCHAR(19) NULL;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HR.tbl_ResearcherProfiles') AND name = 'OrcidEnteredAt' AND is_nullable = 0)
    ALTER TABLE HR.tbl_ResearcherProfiles ALTER COLUMN OrcidEnteredAt DATETIME2 NULL;

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('HR.tbl_ResearcherProfiles') AND name = 'UQ_ResearcherProfiles_Orcid')
    ALTER TABLE HR.tbl_ResearcherProfiles DROP CONSTRAINT UQ_ResearcherProfiles_Orcid;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('HR.tbl_ResearcherProfiles') AND name = 'UX_ResearcherProfiles_Orcid')
    CREATE UNIQUE INDEX UX_ResearcherProfiles_Orcid ON HR.tbl_ResearcherProfiles (Orcid) WHERE Orcid IS NOT NULL;
