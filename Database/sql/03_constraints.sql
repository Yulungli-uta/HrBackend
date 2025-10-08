-- Source: 01_1-ScriptHojaVida.sql
/* =========================================================
   CLAVES PRIMARIAS PARA TODAS LAS TABLAS
   ========================================================= */


-- Tablas geográficas
ALTER TABLE HR.tbl_Countries ADD CONSTRAINT PK_Countries PRIMARY KEY (CountryID);
ALTER TABLE HR.tbl_Provinces ADD CONSTRAINT PK_Provinces PRIMARY KEY (ProvinceID);
ALTER TABLE HR.tbl_Cantons ADD CONSTRAINT PK_Cantons PRIMARY KEY (CantonID);
GO

-- Source: 01_1-ScriptHojaVida.sql
-- Tablas de historia de vida
ALTER TABLE HR.tbl_Addresses ADD CONSTRAINT PK_Addresses PRIMARY KEY (AddressID);
ALTER TABLE HR.tbl_Institutions ADD CONSTRAINT PK_Institutions PRIMARY KEY (InstitutionID);
ALTER TABLE HR.tbl_EducationLevels ADD CONSTRAINT PK_EducationLevels PRIMARY KEY (EducationID);
ALTER TABLE HR.tbl_EmergencyContacts ADD CONSTRAINT PK_EmergencyContacts PRIMARY KEY (ContactID);
ALTER TABLE HR.tbl_CatastrophicIllnesses ADD CONSTRAINT PK_CatastrophicIllnesses PRIMARY KEY (IllnessID);
ALTER TABLE HR.tbl_FamilyBurden ADD CONSTRAINT PK_FamilyBurden PRIMARY KEY (BurdenID);
ALTER TABLE HR.tbl_Trainings ADD CONSTRAINT PK_Trainings PRIMARY KEY (TrainingID);
ALTER TABLE HR.tbl_WorkExperiences ADD CONSTRAINT PK_WorkExperiences PRIMARY KEY (WorkExpID);
ALTER TABLE HR.tbl_BankAccounts ADD CONSTRAINT PK_BankAccounts PRIMARY KEY (AccountID);
ALTER TABLE HR.tbl_Publications ADD CONSTRAINT PK_Publications PRIMARY KEY (PublicationID);
ALTER TABLE HR.tbl_Books ADD CONSTRAINT PK_Books PRIMARY KEY (BookID);
GO

-- Source: 01_1-ScriptHojaVida.sql
/* =========================================================
   RESTRICCIONES UNICAS
   ========================================================= */
ALTER TABLE HR.ref_Types ADD CONSTRAINT UQ_TypeCategoryName UNIQUE (Category, Name);
-- Removidas las restricciones únicas de CountryCode, ProvinceCode, CantonCode ya que estos campos no existen
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 4. AGREGAR PRIMARY KEYS
-------------------------------------------------------------------------------

-- 4.1 Primary Key para tbl_Holidays
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Holidays')
    ALTER TABLE HR.tbl_Holidays ADD CONSTRAINT PK_Holidays PRIMARY KEY (HolidayID);

-- 4.2 Primary Key para tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanning')
    ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT PK_TimePlanning PRIMARY KEY (PlanID);

-- 4.3 Primary Key para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningEmployees')
    ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT PK_TimePlanningEmployees PRIMARY KEY (PlanEmployeeID);

-- 4.4 Primary Key para tbl_TimePlanningExecution
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_TimePlanningExecution')
    ALTER TABLE HR.tbl_TimePlanningExecution ADD CONSTRAINT PK_TimePlanningExecution PRIMARY KEY (ExecutionID);

PRINT 'Primary Keys agregados exitosamente';
GO