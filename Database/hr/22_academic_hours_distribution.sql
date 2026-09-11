-- ============================================================
-- Distributivo de horas académicas (para reporte SIIES Profesores,
-- matriz 5.4 Distribución de Horas)
-- Generado: 2026-09-10
--
-- Origen real de los datos: servidor 10.102.12.3 (bases utamatico /
-- utamatico_informes / dbContratos), sistema "UTA Mático" — NO forma
-- parte de HrBackend. No existe linked server entre 10.102.12.3 y
-- 10.102.12.83, así que esta tabla se llena por carga puntual (consulta
-- en el origen -> INSERT aquí), no por vista en vivo entre servidores.
--
-- Vínculo hacia HR: HR.IDCard = serial_prof/PROCEDULA (cédula) del origen.
-- Aún NO se conecta a HR.vw_SiiesProfesores en este script — eso queda
-- para un paso posterior, una vez confirmada la carga inicial.
--
-- Recarga: la UNIQUE (IDCard, PeriodCode) permite recargar sin duplicar
-- cuando llegue un período académico nuevo (48, 49, ...). El mecanismo de
-- recarga automática por período queda pendiente, a implementar después.
--
-- Solo aditivo / idempotente — seguro de re-ejecutar.
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('[HR].[tbl_AcademicHoursDistribution]') IS NULL
BEGIN
    CREATE TABLE [HR].[tbl_AcademicHoursDistribution]
    (
        [AcademicHoursDistributionId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [IDCard]                 VARCHAR(20) NOT NULL,
        [PeriodCode]             VARCHAR(10) NOT NULL,
        [PeriodStart]            DATE NULL,
        [PeriodEnd]              DATE NULL,
        [ContractCode]           NVARCHAR(100) NULL,
        [ContractsInPeriod]      NVARCHAR(MAX) NULL,
        [NumContractsInPeriod]   INT NOT NULL DEFAULT(0),
        [TotalHours]             INT NOT NULL DEFAULT(0),
        [ClassHours]             INT NOT NULL DEFAULT(0),
        [ManagementHours]        INT NOT NULL DEFAULT(0),
        [ResearchHours]          INT NOT NULL DEFAULT(0),
        [OtherActivitiesHours]   INT NOT NULL DEFAULT(0),
        [TutoringHours]          INT NOT NULL DEFAULT(0),
        [OutreachHours]          INT NOT NULL DEFAULT(0),
        [NullHours]              INT NOT NULL DEFAULT(0),
        [LoadedAt]               DATETIME2 NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT [UX_AcademicHoursDistribution_IDCard_Period] UNIQUE ([IDCard], [PeriodCode])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AcademicHoursDistribution_IDCard' AND object_id = OBJECT_ID('[HR].[tbl_AcademicHoursDistribution]'))
BEGIN
    CREATE INDEX [IX_AcademicHoursDistribution_IDCard] ON [HR].[tbl_AcademicHoursDistribution]([IDCard]);
END
GO
