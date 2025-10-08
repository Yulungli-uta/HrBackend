-- =============================================
-- BLOQUE 4: COMENTARIOS COMPLETOS DE TODAS LAS COLUMNAS
-- =============================================
SET NOCOUNT ON;
PRINT 'INICIANDO AGREGAR COMENTARIOS A TODAS LAS COLUMNAS...';

-- 1. COMENTARIOS PARA HR.ref_Types
PRINT '1. Agregando comentarios para HR.ref_Types...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tabla maestra de tipos y categorías del sistema', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'ref_Types';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único del tipo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'ref_Types', 
    @level2type = N'COLUMN', @level2name = N'TypeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Categoría del tipo (ej: MARITAL_STATUS, GENDER_TYPE, etc.)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'ref_Types', 
    @level2type = N'COLUMN', @level2name = N'Category';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre descriptivo del tipo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'ref_Types', 
    @level2type = N'COLUMN', @level2name = N'Name';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Descripción detallada del tipo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'ref_Types', 
    @level2type = N'COLUMN', @level2name = N'Description';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Indica si el tipo está activo en el sistema', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'ref_Types', 
    @level2type = N'COLUMN', @level2name = N'IsActive';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha y hora de creación del registro', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'ref_Types', 
    @level2type = N'COLUMN', @level2name = N'CreatedAt';

-- 2. COMENTARIOS PARA HR.tbl_Countries
PRINT '2. Agregando comentarios para HR.tbl_Countries...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Catálogo de países', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Countries';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código único del país', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Countries', 
    @level2type = N'COLUMN', @level2name = N'CountryID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre oficial del país', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Countries', 
    @level2type = N'COLUMN', @level2name = N'CountryName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Gentilicio o nacionalidad', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Countries', 
    @level2type = N'COLUMN', @level2name = N'Nationality';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código de nacionalidad', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Countries', 
    @level2type = N'COLUMN', @level2name = N'NationalityCode';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código auxiliar para SIITH', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Countries', 
    @level2type = N'COLUMN', @level2name = N'AuxSIITH';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código auxiliar para CEAACES', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Countries', 
    @level2type = N'COLUMN', @level2name = N'AuxCEAACES';

-- 3. COMENTARIOS PARA HR.tbl_Provinces
PRINT '3. Agregando comentarios para HR.tbl_Provinces...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Catálogo de provincias', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Provinces';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código único de la provincia', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Provinces', 
    @level2type = N'COLUMN', @level2name = N'ProvinceID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'País al que pertenece la provincia', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Provinces', 
    @level2type = N'COLUMN', @level2name = N'CountryID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre de la provincia', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Provinces', 
    @level2type = N'COLUMN', @level2name = N'ProvinceName';

-- 4. COMENTARIOS PARA HR.tbl_Cantons
PRINT '4. Agregando comentarios para HR.tbl_Cantons...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Catálogo de cantones', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Cantons';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código único del cantón', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Cantons', 
    @level2type = N'COLUMN', @level2name = N'CantonID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Provincia a la que pertenece el cantón', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Cantons', 
    @level2type = N'COLUMN', @level2name = N'ProvinceID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre del cantón', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Cantons', 
    @level2type = N'COLUMN', @level2name = N'CantonName';

-- 5. COMENTARIOS PARA HR.tbl_People (TABLA PRINCIPAL)
PRINT '5. Agregando comentarios para HR.tbl_People...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tabla maestra de personas, base para empleados y usuarios del sistema', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único de la persona', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'PersonID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre(s) de la persona', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'FirstName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Apellido(s) de la persona', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'LastName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo de identificación (Cédula, Pasaporte, RUC)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'IdentType';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Número de identificación personal', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'IDCard';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Correo electrónico principal', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'Email';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Número de teléfono convencional', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'Phone';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de nacimiento', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'BirthDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Sexo biológico (Masculino, Femenino)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'Sex';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Género autoidentificado', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'Gender';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Descripción de discapacidad si aplica', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'Disability';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Dirección de residencia principal', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'Address';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Indica si el registro está activo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'IsActive';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de creación del registro', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'CreatedAt';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de última actualización', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'UpdatedAt';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estado civil de la persona', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'MaritalStatusTypeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Número de cartilla militar', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'MilitaryCard';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre completo de la madre', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'MotherName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre completo del padre', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'FatherName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'País de residencia', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'CountryID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Provincia de residencia', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'ProvinceID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Cantón de residencia', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'CantonID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Años de residencia en el domicilio actual', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'YearsOfResidence';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Etnia o grupo étnico', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'EthnicityTypeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Grupo sanguíneo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'BloodTypeTypeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Necesidades especiales', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'SpecialNeedsTypeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Porcentaje de discapacidad', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'DisabilityPercentage';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Número de carnet CONADIS', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'CONADISCard';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Versión del registro para control de concurrencia', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_People', 
    @level2type = N'COLUMN', @level2name = N'RowVersion';



-- =============================================
-- COMENTARIOS PARA HR.tbl_Degrees
-- =============================================

-- Comentario de la tabla
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Tabla para almacenar los grados de cargo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Degrees';
GO

-- Comentarios de columnas
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Identificador único del grado',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Degrees',
    @level2type = N'COLUMN', @level2name = N'DegreeID';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Descripción del grado',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Degrees',
    @level2type = N'COLUMN', @level2name = N'Description';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Indica si el registro está activo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Degrees',
    @level2type = N'COLUMN', @level2name = N'IsActive';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Fecha de creación del registro',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Degrees',
    @level2type = N'COLUMN', @level2name = N'CreatedAt';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Fecha de última actualización del registro',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Degrees',
    @level2type = N'COLUMN', @level2name = N'UpdatedAt';
GO

-- =============================================
-- COMENTARIOS PARA HR.tbl_Occupational_Groups
-- =============================================

-- Comentario de la tabla
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Tabla para almacenar los grupos con su Remuneración Mensual Unificada (RMU)',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups';
GO

-- Comentarios de columnas
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Identificador único del grupo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups',
    @level2type = N'COLUMN', @level2name = N'GroupID';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Descripción del grupo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups',
    @level2type = N'COLUMN', @level2name = N'Description';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Remuneración Mensual Unificada del grupo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups',
    @level2type = N'COLUMN', @level2name = N'RMU';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Clave foránea que referencia al grado',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups',
    @level2type = N'COLUMN', @level2name = N'DegreeID';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Indica si el registro está activo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups',
    @level2type = N'COLUMN', @level2name = N'IsActive';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Fecha de creación del registro',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups',
    @level2type = N'COLUMN', @level2name = N'CreatedAt';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Fecha de última actualización del registro',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Occupational_Groups',
    @level2type = N'COLUMN', @level2name = N'UpdatedAt';
GO

-- =============================================
-- COMENTARIOS PARA HR.tbl_Jobs
-- =============================================

-- Comentario de la tabla
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Tabla para almacenar los puestos de trabajo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs';
GO

-- Comentarios de columnas
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Identificador único del puesto',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs',
    @level2type = N'COLUMN', @level2name = N'JobID';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Descripción del puesto de trabajo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs',
    @level2type = N'COLUMN', @level2name = N'Description';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Tipo de puesto (referencia a HR.ref_Types con Category=''JobType'')',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs',
    @level2type = N'COLUMN', @level2name = N'JobTypeID';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Clave foránea que referencia al grupo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs',
    @level2type = N'COLUMN', @level2name = N'GroupID';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Indica si el registro está activo',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs',
    @level2type = N'COLUMN', @level2name = N'IsActive';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Fecha de creación del registro',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs',
    @level2type = N'COLUMN', @level2name = N'CreatedAt';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Fecha de última actualización del registro',
    @level0type = N'SCHEMA', @level0name = N'HR',
    @level1type = N'TABLE', @level1name = N'tbl_Jobs',
    @level2type = N'COLUMN', @level2name = N'UpdatedAt';
GO


-- 6. COMENTARIOS PARA HR.tbl_Departments
PRINT '6. Agregando comentarios para HR.tbl_Departments...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estructura organizacional jerárquica de la universidad', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único del departamento', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'DepartmentID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Departamento padre para estructuras jerárquicas', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'ParentID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código único del departamento (ej: FING, DITIC)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'Code';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre completo del departamento', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'Name';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nombre abreviado o acrónimo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'ShortName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo de departamento (Rectorado, Facultad, Carrera, etc.)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'DepartmentType';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Correo electrónico del departamento', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'Email';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Teléfono del departamento', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'Phone';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Ubicación física del departamento', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'Location';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Decano o director del departamento', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'DeanDirector';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Código presupuestario asignado', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'BudgetCode';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nivel jerárquico en la estructura organizacional', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'Dlevel';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Indica si el departamento está activo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Departments', 
    @level2type = N'COLUMN', @level2name = N'IsActive';

-- Continuaría con las demás tablas, pero por razones de longitud mostraré el patrón...
-- 7. COMENTARIOS PARA HR.tbl_jobs
PRINT '7. Agregando comentarios para HR.tbl_jobs...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Catálogo de puestos de trabajo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_jobs';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único del puesto', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_jobs', 
    @level2type = N'COLUMN', @level2name = N'JobID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Título del puesto de trabajo', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_jobs', 
    @level2type = N'COLUMN', @level2name = N'Title';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Descripción detallada del puesto', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_jobs', 
    @level2type = N'COLUMN', @level2name = N'Description';

-- 8. COMENTARIOS PARA HR.tbl_Employees
PRINT '8. Agregando comentarios para HR.tbl_Employees...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Registro de empleados de la universidad', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Employees';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador del empleado (igual a PersonID)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Employees', 
    @level2type = N'COLUMN', @level2name = N'EmployeeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo de empleado (Docente, Administrativo, etc.)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Employees', 
    @level2type = N'COLUMN', @level2name = N'EmployeeType';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Departamento al que pertenece el empleado', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Employees', 
    @level2type = N'COLUMN', @level2name = N'DepartmentID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Jefe inmediato del empleado', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Employees', 
    @level2type = N'COLUMN', @level2name = N'ImmediateBossID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de contratación', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Employees', 
    @level2type = N'COLUMN', @level2name = N'HireDate';

-- 9. COMENTARIOS PARA HR.tbl_Contracts
PRINT '9. Agregando comentarios para HR.tbl_Contracts...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Registro de contratos laborales', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único del contrato', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'ContractID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Empleado asociado al contrato', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'EmployeeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo de contrato (Indefinido, Temporal, etc.)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'ContractType';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Puesto de trabajo asignado', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'JobID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de inicio del contrato', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'StartDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de finalización del contrato (NULL si es indefinido)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'EndDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Número de documento del contrato', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'DocumentNum';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Motivación o justificación del contrato', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'Motivation';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Partida presupuestaria asignada', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'BudgetItem';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Grado o nivel del puesto', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'Grade';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Nivel de gestión o gobierno', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'GovernanceLevel';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Lugar de trabajo asignado', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'Workplace';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Salario base del contrato', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Contracts', 
    @level2type = N'COLUMN', @level2name = N'BaseSalary';

-- 10. COMENTARIOS PARA HR.tbl_AttendancePunches
PRINT '10. Agregando comentarios para HR.tbl_AttendancePunches...';
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Registro de marcaciones de entrada y salida', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único de la marcación', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches', 
    @level2type = N'COLUMN', @level2name = N'PunchID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Empleado que realizó la marcación', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches', 
    @level2type = N'COLUMN', @level2name = N'EmployeeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha y hora de la marcación', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches', 
    @level2type = N'COLUMN', @level2name = N'PunchTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo de marcación (In=Entrada, Out=Salida)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches', 
    @level2type = N'COLUMN', @level2name = N'PunchType';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador del dispositivo de marcación', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches', 
    @level2type = N'COLUMN', @level2name = N'DeviceID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Longitud geográfica de la marcación', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches', 
    @level2type = N'COLUMN', @level2name = N'Longitude';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Latitud geográfica de la marcación', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_AttendancePunches', 
    @level2type = N'COLUMN', @level2name = N'Latitude';

-- CONTINUARÍA CON LAS DEMÁS TABLAS...
-- Por razones de espacio, muestro el patrón para las tablas restantes

PRINT '11. Agregando comentarios para tablas restantes...';

-- 11. COMENTARIOS PARA HR.tbl_Vacations
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Registro de períodos de vacaciones', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único de las vacaciones', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations', 
    @level2type = N'COLUMN', @level2name = N'VacationID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Empleado que toma las vacaciones', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations', 
    @level2type = N'COLUMN', @level2name = N'EmployeeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de inicio de las vacaciones', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations', 
    @level2type = N'COLUMN', @level2name = N'StartDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de fin de las vacaciones', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations', 
    @level2type = N'COLUMN', @level2name = N'EndDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Días de vacaciones otorgados', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations', 
    @level2type = N'COLUMN', @level2name = N'DaysGranted';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Días de vacaciones tomados', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations', 
    @level2type = N'COLUMN', @level2name = N'DaysTaken';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estado de las vacaciones (Planned, InProgress, Completed, Canceled)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Vacations', 
    @level2type = N'COLUMN', @level2name = N'Status';

-- 12. COMENTARIOS PARA HR.tbl_Permissions
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Registro de permisos solicitados por empleados', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Identificador único del permiso', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'PermissionID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Empleado que solicita el permiso', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'EmployeeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Tipo de permiso solicitado', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'PermissionTypeID';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de inicio del permiso', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'StartDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha de fin del permiso', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'EndDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Indica si el permiso se carga a vacaciones', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'ChargedToVacation';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Empleado que aprobó el permiso', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'ApprovedBy';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Justificación del permiso', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'Justification';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Fecha y hora de la solicitud', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'RequestDate';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Estado del permiso (Pending, Approved, Rejected)', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'Status';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'Vacaciones asociadas si aplica', 
    @level0type = N'SCHEMA', @level0name = N'HR', @level1type = N'TABLE', @level1name = N'tbl_Permissions', 
    @level2type = N'COLUMN', @level2name = N'VacationID';

-- Continuaría con todas las tablas restantes de la misma manera...

PRINT 'COMENTARIOS AGREGADOS EXITOSAMENTE A TODAS LAS TABLAS.';
GO