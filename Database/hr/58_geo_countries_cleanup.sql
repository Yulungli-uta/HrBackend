-- Catálogo geográfico: limpieza y completado de PAÍSES (ISO 3166-1).
-- 1) Fusiona duplicados y entradas que no son países, repuntando TODAS las referencias (queda registro en HR.tbl_GeoCleanupLog).
-- 2) Quita el país de prueba QA de capacitaciones y libros reales (sin borrarlos) y elimina los países y provincias de QA.
-- 3) Marca el código ISO de cada país y agrega los que faltaban.
SET XACT_ABORT ON;
BEGIN TRAN;
-- 022 BELGICA -> 017 (BE)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'022' WHERE c.CountryID = N'017';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_People WHERE CountryID = N'022';
UPDATE HR.tbl_People SET CountryID = N'017' WHERE CountryID = N'022';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_Addresses WHERE CountryID = N'022';
UPDATE HR.tbl_Addresses SET CountryID = N'017' WHERE CountryID = N'022';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_Institutions WHERE CountryID = N'022';
UPDATE HR.tbl_Institutions SET CountryID = N'017' WHERE CountryID = N'022';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_Trainings WHERE CountryID = N'022';
UPDATE HR.tbl_Trainings SET CountryID = N'017' WHERE CountryID = N'022';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_Books WHERE CountryID = N'022';
UPDATE HR.tbl_Books SET CountryID = N'017' WHERE CountryID = N'022';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_Languages WHERE CountryID = N'022';
UPDATE HR.tbl_Languages SET CountryID = N'017' WHERE CountryID = N'022';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_WorkExperiences WHERE CountryID = N'022';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'017' WHERE CountryID = N'022';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'017', N'Fusion pais 022->017' FROM HR.tbl_Provinces WHERE CountryID = N'022';
UPDATE HR.tbl_Provinces SET CountryID = N'017' WHERE CountryID = N'022';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'022';
-- 025 BERMUDAS -> 018 (BM)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'025' WHERE c.CountryID = N'018';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_People WHERE CountryID = N'025';
UPDATE HR.tbl_People SET CountryID = N'018' WHERE CountryID = N'025';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_Addresses WHERE CountryID = N'025';
UPDATE HR.tbl_Addresses SET CountryID = N'018' WHERE CountryID = N'025';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_Institutions WHERE CountryID = N'025';
UPDATE HR.tbl_Institutions SET CountryID = N'018' WHERE CountryID = N'025';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_Trainings WHERE CountryID = N'025';
UPDATE HR.tbl_Trainings SET CountryID = N'018' WHERE CountryID = N'025';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_Books WHERE CountryID = N'025';
UPDATE HR.tbl_Books SET CountryID = N'018' WHERE CountryID = N'025';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_Languages WHERE CountryID = N'025';
UPDATE HR.tbl_Languages SET CountryID = N'018' WHERE CountryID = N'025';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_WorkExperiences WHERE CountryID = N'025';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'018' WHERE CountryID = N'025';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'018', N'Fusion pais 025->018' FROM HR.tbl_Provinces WHERE CountryID = N'025';
UPDATE HR.tbl_Provinces SET CountryID = N'018' WHERE CountryID = N'025';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'025';
-- 027 BOLIVIA -> 019 (BO)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'027' WHERE c.CountryID = N'019';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_People WHERE CountryID = N'027';
UPDATE HR.tbl_People SET CountryID = N'019' WHERE CountryID = N'027';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_Addresses WHERE CountryID = N'027';
UPDATE HR.tbl_Addresses SET CountryID = N'019' WHERE CountryID = N'027';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_Institutions WHERE CountryID = N'027';
UPDATE HR.tbl_Institutions SET CountryID = N'019' WHERE CountryID = N'027';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_Trainings WHERE CountryID = N'027';
UPDATE HR.tbl_Trainings SET CountryID = N'019' WHERE CountryID = N'027';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_Books WHERE CountryID = N'027';
UPDATE HR.tbl_Books SET CountryID = N'019' WHERE CountryID = N'027';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_Languages WHERE CountryID = N'027';
UPDATE HR.tbl_Languages SET CountryID = N'019' WHERE CountryID = N'027';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_WorkExperiences WHERE CountryID = N'027';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'019' WHERE CountryID = N'027';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'019', N'Fusion pais 027->019' FROM HR.tbl_Provinces WHERE CountryID = N'027';
UPDATE HR.tbl_Provinces SET CountryID = N'019' WHERE CountryID = N'027';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'027';
-- 028 BOSNIA -> 020 (BA)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'028' WHERE c.CountryID = N'020';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_People WHERE CountryID = N'028';
UPDATE HR.tbl_People SET CountryID = N'020' WHERE CountryID = N'028';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_Addresses WHERE CountryID = N'028';
UPDATE HR.tbl_Addresses SET CountryID = N'020' WHERE CountryID = N'028';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_Institutions WHERE CountryID = N'028';
UPDATE HR.tbl_Institutions SET CountryID = N'020' WHERE CountryID = N'028';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_Trainings WHERE CountryID = N'028';
UPDATE HR.tbl_Trainings SET CountryID = N'020' WHERE CountryID = N'028';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_Books WHERE CountryID = N'028';
UPDATE HR.tbl_Books SET CountryID = N'020' WHERE CountryID = N'028';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_Languages WHERE CountryID = N'028';
UPDATE HR.tbl_Languages SET CountryID = N'020' WHERE CountryID = N'028';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_WorkExperiences WHERE CountryID = N'028';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'020' WHERE CountryID = N'028';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 028->020' FROM HR.tbl_Provinces WHERE CountryID = N'028';
UPDATE HR.tbl_Provinces SET CountryID = N'020' WHERE CountryID = N'028';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'028';
-- 087 HERZEGOVINA -> 020 (BA)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'087' WHERE c.CountryID = N'020';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_People WHERE CountryID = N'087';
UPDATE HR.tbl_People SET CountryID = N'020' WHERE CountryID = N'087';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_Addresses WHERE CountryID = N'087';
UPDATE HR.tbl_Addresses SET CountryID = N'020' WHERE CountryID = N'087';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_Institutions WHERE CountryID = N'087';
UPDATE HR.tbl_Institutions SET CountryID = N'020' WHERE CountryID = N'087';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_Trainings WHERE CountryID = N'087';
UPDATE HR.tbl_Trainings SET CountryID = N'020' WHERE CountryID = N'087';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_Books WHERE CountryID = N'087';
UPDATE HR.tbl_Books SET CountryID = N'020' WHERE CountryID = N'087';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_Languages WHERE CountryID = N'087';
UPDATE HR.tbl_Languages SET CountryID = N'020' WHERE CountryID = N'087';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_WorkExperiences WHERE CountryID = N'087';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'020' WHERE CountryID = N'087';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'020', N'Fusion pais 087->020' FROM HR.tbl_Provinces WHERE CountryID = N'087';
UPDATE HR.tbl_Provinces SET CountryID = N'020' WHERE CountryID = N'087';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'087';
-- 029 BOTSWANA -> 021 (BW)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'029' WHERE c.CountryID = N'021';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_People WHERE CountryID = N'029';
UPDATE HR.tbl_People SET CountryID = N'021' WHERE CountryID = N'029';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_Addresses WHERE CountryID = N'029';
UPDATE HR.tbl_Addresses SET CountryID = N'021' WHERE CountryID = N'029';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_Institutions WHERE CountryID = N'029';
UPDATE HR.tbl_Institutions SET CountryID = N'021' WHERE CountryID = N'029';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_Trainings WHERE CountryID = N'029';
UPDATE HR.tbl_Trainings SET CountryID = N'021' WHERE CountryID = N'029';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_Books WHERE CountryID = N'029';
UPDATE HR.tbl_Books SET CountryID = N'021' WHERE CountryID = N'029';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_Languages WHERE CountryID = N'029';
UPDATE HR.tbl_Languages SET CountryID = N'021' WHERE CountryID = N'029';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_WorkExperiences WHERE CountryID = N'029';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'021' WHERE CountryID = N'029';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'021', N'Fusion pais 029->021' FROM HR.tbl_Provinces WHERE CountryID = N'029';
UPDATE HR.tbl_Provinces SET CountryID = N'021' WHERE CountryID = N'029';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'029';
-- 218 REPUBLICA CHECA -> 041 (CZ)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'218' WHERE c.CountryID = N'041';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_People WHERE CountryID = N'218';
UPDATE HR.tbl_People SET CountryID = N'041' WHERE CountryID = N'218';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_Addresses WHERE CountryID = N'218';
UPDATE HR.tbl_Addresses SET CountryID = N'041' WHERE CountryID = N'218';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_Institutions WHERE CountryID = N'218';
UPDATE HR.tbl_Institutions SET CountryID = N'041' WHERE CountryID = N'218';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_Trainings WHERE CountryID = N'218';
UPDATE HR.tbl_Trainings SET CountryID = N'041' WHERE CountryID = N'218';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_Books WHERE CountryID = N'218';
UPDATE HR.tbl_Books SET CountryID = N'041' WHERE CountryID = N'218';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_Languages WHERE CountryID = N'218';
UPDATE HR.tbl_Languages SET CountryID = N'041' WHERE CountryID = N'218';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_WorkExperiences WHERE CountryID = N'218';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'041' WHERE CountryID = N'218';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'041', N'Fusion pais 218->041' FROM HR.tbl_Provinces WHERE CountryID = N'218';
UPDATE HR.tbl_Provinces SET CountryID = N'041' WHERE CountryID = N'218';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'218';
-- 195 TÍBET ( BAJO SOBERANÍA CHINA) -> 043 (CN)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'195' WHERE c.CountryID = N'043';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_People WHERE CountryID = N'195';
UPDATE HR.tbl_People SET CountryID = N'043' WHERE CountryID = N'195';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_Addresses WHERE CountryID = N'195';
UPDATE HR.tbl_Addresses SET CountryID = N'043' WHERE CountryID = N'195';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_Institutions WHERE CountryID = N'195';
UPDATE HR.tbl_Institutions SET CountryID = N'043' WHERE CountryID = N'195';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_Trainings WHERE CountryID = N'195';
UPDATE HR.tbl_Trainings SET CountryID = N'043' WHERE CountryID = N'195';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_Books WHERE CountryID = N'195';
UPDATE HR.tbl_Books SET CountryID = N'043' WHERE CountryID = N'195';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_Languages WHERE CountryID = N'195';
UPDATE HR.tbl_Languages SET CountryID = N'043' WHERE CountryID = N'195';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_WorkExperiences WHERE CountryID = N'195';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'043' WHERE CountryID = N'195';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'043', N'Fusion pais 195->043' FROM HR.tbl_Provinces WHERE CountryID = N'195';
UPDATE HR.tbl_Provinces SET CountryID = N'043' WHERE CountryID = N'195';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'195';
-- 164 REPÚBLICA DEL CONGO -> 047 (CG)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'164' WHERE c.CountryID = N'047';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_People WHERE CountryID = N'164';
UPDATE HR.tbl_People SET CountryID = N'047' WHERE CountryID = N'164';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_Addresses WHERE CountryID = N'164';
UPDATE HR.tbl_Addresses SET CountryID = N'047' WHERE CountryID = N'164';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_Institutions WHERE CountryID = N'164';
UPDATE HR.tbl_Institutions SET CountryID = N'047' WHERE CountryID = N'164';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_Trainings WHERE CountryID = N'164';
UPDATE HR.tbl_Trainings SET CountryID = N'047' WHERE CountryID = N'164';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_Books WHERE CountryID = N'164';
UPDATE HR.tbl_Books SET CountryID = N'047' WHERE CountryID = N'164';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_Languages WHERE CountryID = N'164';
UPDATE HR.tbl_Languages SET CountryID = N'047' WHERE CountryID = N'164';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_WorkExperiences WHERE CountryID = N'164';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'047' WHERE CountryID = N'164';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'047', N'Fusion pais 164->047' FROM HR.tbl_Provinces WHERE CountryID = N'164';
UPDATE HR.tbl_Provinces SET CountryID = N'047' WHERE CountryID = N'164';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'164';
-- 057 DUBAI -> 060 (AE)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'057' WHERE c.CountryID = N'060';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_People WHERE CountryID = N'057';
UPDATE HR.tbl_People SET CountryID = N'060' WHERE CountryID = N'057';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_Addresses WHERE CountryID = N'057';
UPDATE HR.tbl_Addresses SET CountryID = N'060' WHERE CountryID = N'057';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_Institutions WHERE CountryID = N'057';
UPDATE HR.tbl_Institutions SET CountryID = N'060' WHERE CountryID = N'057';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_Trainings WHERE CountryID = N'057';
UPDATE HR.tbl_Trainings SET CountryID = N'060' WHERE CountryID = N'057';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_Books WHERE CountryID = N'057';
UPDATE HR.tbl_Books SET CountryID = N'060' WHERE CountryID = N'057';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_Languages WHERE CountryID = N'057';
UPDATE HR.tbl_Languages SET CountryID = N'060' WHERE CountryID = N'057';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_WorkExperiences WHERE CountryID = N'057';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'060' WHERE CountryID = N'057';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'060', N'Fusion pais 057->060' FROM HR.tbl_Provinces WHERE CountryID = N'057';
UPDATE HR.tbl_Provinces SET CountryID = N'060' WHERE CountryID = N'057';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'057';
-- 062 ESCOCIA -> 162 (GB)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'062' WHERE c.CountryID = N'162';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_People WHERE CountryID = N'062';
UPDATE HR.tbl_People SET CountryID = N'162' WHERE CountryID = N'062';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_Addresses WHERE CountryID = N'062';
UPDATE HR.tbl_Addresses SET CountryID = N'162' WHERE CountryID = N'062';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_Institutions WHERE CountryID = N'062';
UPDATE HR.tbl_Institutions SET CountryID = N'162' WHERE CountryID = N'062';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_Trainings WHERE CountryID = N'062';
UPDATE HR.tbl_Trainings SET CountryID = N'162' WHERE CountryID = N'062';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_Books WHERE CountryID = N'062';
UPDATE HR.tbl_Books SET CountryID = N'162' WHERE CountryID = N'062';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_Languages WHERE CountryID = N'062';
UPDATE HR.tbl_Languages SET CountryID = N'162' WHERE CountryID = N'062';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_WorkExperiences WHERE CountryID = N'062';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'162' WHERE CountryID = N'062';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 062->162' FROM HR.tbl_Provinces WHERE CountryID = N'062';
UPDATE HR.tbl_Provinces SET CountryID = N'162' WHERE CountryID = N'062';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'062';
-- 217 INGLATERRA -> 162 (GB)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'217' WHERE c.CountryID = N'162';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_People WHERE CountryID = N'217';
UPDATE HR.tbl_People SET CountryID = N'162' WHERE CountryID = N'217';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_Addresses WHERE CountryID = N'217';
UPDATE HR.tbl_Addresses SET CountryID = N'162' WHERE CountryID = N'217';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_Institutions WHERE CountryID = N'217';
UPDATE HR.tbl_Institutions SET CountryID = N'162' WHERE CountryID = N'217';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_Trainings WHERE CountryID = N'217';
UPDATE HR.tbl_Trainings SET CountryID = N'162' WHERE CountryID = N'217';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_Books WHERE CountryID = N'217';
UPDATE HR.tbl_Books SET CountryID = N'162' WHERE CountryID = N'217';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_Languages WHERE CountryID = N'217';
UPDATE HR.tbl_Languages SET CountryID = N'162' WHERE CountryID = N'217';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_WorkExperiences WHERE CountryID = N'217';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'162' WHERE CountryID = N'217';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'162', N'Fusion pais 217->162' FROM HR.tbl_Provinces WHERE CountryID = N'217';
UPDATE HR.tbl_Provinces SET CountryID = N'162' WHERE CountryID = N'217';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'217';
-- 088 HOLANDA -> 151 (NL)
UPDATE c SET c.NationalityCode = COALESCE(c.NationalityCode, x.NationalityCode), c.Nationality = COALESCE(c.Nationality, x.Nationality), c.AuxSIITH = COALESCE(c.AuxSIITH, x.AuxSIITH), c.AuxCEAACES = COALESCE(c.AuxCEAACES, x.AuxCEAACES) FROM HR.tbl_Countries c JOIN HR.tbl_Countries x ON x.CountryID = N'088' WHERE c.CountryID = N'151';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_People', CAST(PersonID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_People WHERE CountryID = N'088';
UPDATE HR.tbl_People SET CountryID = N'151' WHERE CountryID = N'088';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Addresses', CAST(AddressID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_Addresses WHERE CountryID = N'088';
UPDATE HR.tbl_Addresses SET CountryID = N'151' WHERE CountryID = N'088';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Institutions', CAST(InstitutionID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_Institutions WHERE CountryID = N'088';
UPDATE HR.tbl_Institutions SET CountryID = N'151' WHERE CountryID = N'088';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_Trainings WHERE CountryID = N'088';
UPDATE HR.tbl_Trainings SET CountryID = N'151' WHERE CountryID = N'088';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_Books WHERE CountryID = N'088';
UPDATE HR.tbl_Books SET CountryID = N'151' WHERE CountryID = N'088';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Languages', CAST(LanguageID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_Languages WHERE CountryID = N'088';
UPDATE HR.tbl_Languages SET CountryID = N'151' WHERE CountryID = N'088';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_WorkExperiences', CAST(WorkExpID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_WorkExperiences WHERE CountryID = N'088';
UPDATE HR.tbl_WorkExperiences SET CountryID = N'151' WHERE CountryID = N'088';
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Provinces', CAST(ProvinceID AS nvarchar(50)), N'CountryID', CountryID, N'151', N'Fusion pais 088->151' FROM HR.tbl_Provinces WHERE CountryID = N'088';
UPDATE HR.tbl_Provinces SET CountryID = N'151' WHERE CountryID = N'088';
DELETE FROM HR.tbl_Countries WHERE CountryID = N'088';
-- Países de prueba QA
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Trainings', CAST(TrainingID AS nvarchar(50)), N'CountryID', CountryID, NULL, N'Pais de prueba QA' FROM HR.tbl_Trainings WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233');
UPDATE HR.tbl_Trainings SET CountryID = NULL WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233');
INSERT INTO HR.tbl_GeoCleanupLog (TableName, PkValue, ColumnName, OldValue, NewValue, Reason) SELECT N'tbl_Books', CAST(BookID AS nvarchar(50)), N'CountryID', CountryID, NULL, N'Pais de prueba QA' FROM HR.tbl_Books WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233');
UPDATE HR.tbl_Books SET CountryID = NULL WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233');
IF EXISTS (SELECT 1 FROM HR.tbl_People WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233')) OR EXISTS (SELECT 1 FROM HR.tbl_Addresses WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233')) OR EXISTS (SELECT 1 FROM HR.tbl_Institutions WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233')) OR EXISTS (SELECT 1 FROM HR.tbl_Languages WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233')) OR EXISTS (SELECT 1 FROM HR.tbl_WorkExperiences WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233')) BEGIN ROLLBACK; THROW 50000, 'Hay referencias reales a un pais de QA', 1; END
IF EXISTS (SELECT 1 FROM HR.tbl_Provinces p WHERE p.CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233') AND (EXISTS (SELECT 1 FROM HR.tbl_People WHERE ProvinceID = p.ProvinceID) OR EXISTS (SELECT 1 FROM HR.tbl_Addresses WHERE ProvinceID = p.ProvinceID) OR EXISTS (SELECT 1 FROM HR.tbl_Institutions WHERE ProvinceID = p.ProvinceID))) BEGIN ROLLBACK; THROW 50000, 'Hay referencias reales a una provincia de QA', 1; END
IF EXISTS (SELECT 1 FROM HR.tbl_Cantons k JOIN HR.tbl_Provinces p ON p.ProvinceID = k.ProvinceID WHERE p.CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233') AND (EXISTS (SELECT 1 FROM HR.tbl_People WHERE CantonID = k.CantonID) OR EXISTS (SELECT 1 FROM HR.tbl_Addresses WHERE CantonID = k.CantonID) OR EXISTS (SELECT 1 FROM HR.tbl_Institutions WHERE CantonID = k.CantonID))) BEGIN ROLLBACK; THROW 50000, 'Hay referencias reales a un canton de QA', 1; END
DELETE k FROM HR.tbl_Cantons k JOIN HR.tbl_Provinces p ON p.ProvinceID = k.ProvinceID WHERE p.CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233');
DELETE FROM HR.tbl_Provinces WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233');
DELETE FROM HR.tbl_Countries WHERE CountryID IN (N'220',N'221',N'222',N'223',N'224',N'225',N'226',N'227',N'228',N'229',N'230',N'231',N'232',N'233');
-- Código ISO de los países existentes
UPDATE HR.tbl_Countries SET IsoCode = N'AD' WHERE CountryID = N'006';
UPDATE HR.tbl_Countries SET IsoCode = N'AE' WHERE CountryID = N'060';
UPDATE HR.tbl_Countries SET IsoCode = N'AF' WHERE CountryID = N'003';
UPDATE HR.tbl_Countries SET IsoCode = N'AL' WHERE CountryID = N'004';
UPDATE HR.tbl_Countries SET IsoCode = N'AM' WHERE CountryID = N'011';
UPDATE HR.tbl_Countries SET IsoCode = N'AO' WHERE CountryID = N'007';
UPDATE HR.tbl_Countries SET IsoCode = N'AR' WHERE CountryID = N'010';
UPDATE HR.tbl_Countries SET IsoCode = N'AS' WHERE CountryID = N'173';
UPDATE HR.tbl_Countries SET IsoCode = N'AT' WHERE CountryID = N'014';
UPDATE HR.tbl_Countries SET IsoCode = N'AU' WHERE CountryID = N'013';
UPDATE HR.tbl_Countries SET IsoCode = N'AW' WHERE CountryID = N'012';
UPDATE HR.tbl_Countries SET IsoCode = N'BA' WHERE CountryID = N'020';
UPDATE HR.tbl_Countries SET IsoCode = N'BB' WHERE CountryID = N'016';
UPDATE HR.tbl_Countries SET IsoCode = N'BE' WHERE CountryID = N'017';
UPDATE HR.tbl_Countries SET IsoCode = N'BF' WHERE CountryID = N'033';
UPDATE HR.tbl_Countries SET IsoCode = N'BG' WHERE CountryID = N'032';
UPDATE HR.tbl_Countries SET IsoCode = N'BI' WHERE CountryID = N'034';
UPDATE HR.tbl_Countries SET IsoCode = N'BJ' WHERE CountryID = N'024';
UPDATE HR.tbl_Countries SET IsoCode = N'BM' WHERE CountryID = N'018';
UPDATE HR.tbl_Countries SET IsoCode = N'BN' WHERE CountryID = N'031';
UPDATE HR.tbl_Countries SET IsoCode = N'BO' WHERE CountryID = N'019';
UPDATE HR.tbl_Countries SET IsoCode = N'BR' WHERE CountryID = N'030';
UPDATE HR.tbl_Countries SET IsoCode = N'BS' WHERE CountryID = N'015';
UPDATE HR.tbl_Countries SET IsoCode = N'BT' WHERE CountryID = N'035';
UPDATE HR.tbl_Countries SET IsoCode = N'BW' WHERE CountryID = N'021';
UPDATE HR.tbl_Countries SET IsoCode = N'BY' WHERE CountryID = N'026';
UPDATE HR.tbl_Countries SET IsoCode = N'BZ' WHERE CountryID = N'023';
UPDATE HR.tbl_Countries SET IsoCode = N'CA' WHERE CountryID = N'039';
UPDATE HR.tbl_Countries SET IsoCode = N'CD' WHERE CountryID = N'165';
UPDATE HR.tbl_Countries SET IsoCode = N'CF' WHERE CountryID = N'163';
UPDATE HR.tbl_Countries SET IsoCode = N'CG' WHERE CountryID = N'047';
UPDATE HR.tbl_Countries SET IsoCode = N'CH' WHERE CountryID = N'190';
UPDATE HR.tbl_Countries SET IsoCode = N'CI' WHERE CountryID = N'050';
UPDATE HR.tbl_Countries SET IsoCode = N'CL' WHERE CountryID = N'042';
UPDATE HR.tbl_Countries SET IsoCode = N'CM' WHERE CountryID = N'038';
UPDATE HR.tbl_Countries SET IsoCode = N'CN' WHERE CountryID = N'043';
UPDATE HR.tbl_Countries SET IsoCode = N'CO' WHERE CountryID = N'045';
UPDATE HR.tbl_Countries SET IsoCode = N'CR' WHERE CountryID = N'051';
UPDATE HR.tbl_Countries SET IsoCode = N'CU' WHERE CountryID = N'053';
UPDATE HR.tbl_Countries SET IsoCode = N'CV' WHERE CountryID = N'036';
UPDATE HR.tbl_Countries SET IsoCode = N'CW' WHERE CountryID = N'054';
UPDATE HR.tbl_Countries SET IsoCode = N'CY' WHERE CountryID = N'044';
UPDATE HR.tbl_Countries SET IsoCode = N'CZ' WHERE CountryID = N'041';
UPDATE HR.tbl_Countries SET IsoCode = N'DE' WHERE CountryID = N'005';
UPDATE HR.tbl_Countries SET IsoCode = N'DJ' WHERE CountryID = N'214';
UPDATE HR.tbl_Countries SET IsoCode = N'DK' WHERE CountryID = N'055';
UPDATE HR.tbl_Countries SET IsoCode = N'DM' WHERE CountryID = N'056';
UPDATE HR.tbl_Countries SET IsoCode = N'DO' WHERE CountryID = N'166';
UPDATE HR.tbl_Countries SET IsoCode = N'DZ' WHERE CountryID = N'009';
UPDATE HR.tbl_Countries SET IsoCode = N'EC' WHERE CountryID = N'001';
UPDATE HR.tbl_Countries SET IsoCode = N'EE' WHERE CountryID = N'067';
UPDATE HR.tbl_Countries SET IsoCode = N'EG' WHERE CountryID = N'058';
UPDATE HR.tbl_Countries SET IsoCode = N'EH' WHERE CountryID = N'170';
UPDATE HR.tbl_Countries SET IsoCode = N'ER' WHERE CountryID = N'061';
UPDATE HR.tbl_Countries SET IsoCode = N'ES' WHERE CountryID = N'065';
UPDATE HR.tbl_Countries SET IsoCode = N'ET' WHERE CountryID = N'068';
UPDATE HR.tbl_Countries SET IsoCode = N'FI' WHERE CountryID = N'070';
UPDATE HR.tbl_Countries SET IsoCode = N'FJ' WHERE CountryID = N'071';
UPDATE HR.tbl_Countries SET IsoCode = N'FM' WHERE CountryID = N'134';
UPDATE HR.tbl_Countries SET IsoCode = N'FR' WHERE CountryID = N'072';
UPDATE HR.tbl_Countries SET IsoCode = N'GA' WHERE CountryID = N'073';
UPDATE HR.tbl_Countries SET IsoCode = N'GB' WHERE CountryID = N'162';
UPDATE HR.tbl_Countries SET IsoCode = N'GD' WHERE CountryID = N'077';
UPDATE HR.tbl_Countries SET IsoCode = N'GE' WHERE CountryID = N'075';
UPDATE HR.tbl_Countries SET IsoCode = N'GF' WHERE CountryID = N'081';
UPDATE HR.tbl_Countries SET IsoCode = N'GH' WHERE CountryID = N'076';
UPDATE HR.tbl_Countries SET IsoCode = N'GM' WHERE CountryID = N'074';
UPDATE HR.tbl_Countries SET IsoCode = N'GN' WHERE CountryID = N'082';
UPDATE HR.tbl_Countries SET IsoCode = N'GQ' WHERE CountryID = N'083';
UPDATE HR.tbl_Countries SET IsoCode = N'GR' WHERE CountryID = N'078';
UPDATE HR.tbl_Countries SET IsoCode = N'GT' WHERE CountryID = N'080';
UPDATE HR.tbl_Countries SET IsoCode = N'GU' WHERE CountryID = N'079';
UPDATE HR.tbl_Countries SET IsoCode = N'GW' WHERE CountryID = N'084';
UPDATE HR.tbl_Countries SET IsoCode = N'GY' WHERE CountryID = N'085';
UPDATE HR.tbl_Countries SET IsoCode = N'HK' WHERE CountryID = N'090';
UPDATE HR.tbl_Countries SET IsoCode = N'HN' WHERE CountryID = N'089';
UPDATE HR.tbl_Countries SET IsoCode = N'HR' WHERE CountryID = N'052';
UPDATE HR.tbl_Countries SET IsoCode = N'HT' WHERE CountryID = N'086';
UPDATE HR.tbl_Countries SET IsoCode = N'HU' WHERE CountryID = N'091';
UPDATE HR.tbl_Countries SET IsoCode = N'ID' WHERE CountryID = N'093';
UPDATE HR.tbl_Countries SET IsoCode = N'IE' WHERE CountryID = N'096';
UPDATE HR.tbl_Countries SET IsoCode = N'IL' WHERE CountryID = N'102';
UPDATE HR.tbl_Countries SET IsoCode = N'IN' WHERE CountryID = N'092';
UPDATE HR.tbl_Countries SET IsoCode = N'IQ' WHERE CountryID = N'094';
UPDATE HR.tbl_Countries SET IsoCode = N'IR' WHERE CountryID = N'095';
UPDATE HR.tbl_Countries SET IsoCode = N'IS' WHERE CountryID = N'097';
UPDATE HR.tbl_Countries SET IsoCode = N'IT' WHERE CountryID = N'103';
UPDATE HR.tbl_Countries SET IsoCode = N'JM' WHERE CountryID = N'104';
UPDATE HR.tbl_Countries SET IsoCode = N'JO' WHERE CountryID = N'106';
UPDATE HR.tbl_Countries SET IsoCode = N'JP' WHERE CountryID = N'105';
UPDATE HR.tbl_Countries SET IsoCode = N'KE' WHERE CountryID = N'108';
UPDATE HR.tbl_Countries SET IsoCode = N'KG' WHERE CountryID = N'109';
UPDATE HR.tbl_Countries SET IsoCode = N'KH' WHERE CountryID = N'037';
UPDATE HR.tbl_Countries SET IsoCode = N'KI' WHERE CountryID = N'110';
UPDATE HR.tbl_Countries SET IsoCode = N'KM' WHERE CountryID = N'046';
UPDATE HR.tbl_Countries SET IsoCode = N'KN' WHERE CountryID = N'171';
UPDATE HR.tbl_Countries SET IsoCode = N'KP' WHERE CountryID = N'048';
UPDATE HR.tbl_Countries SET IsoCode = N'KR' WHERE CountryID = N'049';
UPDATE HR.tbl_Countries SET IsoCode = N'KW' WHERE CountryID = N'112';
UPDATE HR.tbl_Countries SET IsoCode = N'KY' WHERE CountryID = N'098';
UPDATE HR.tbl_Countries SET IsoCode = N'KZ' WHERE CountryID = N'107';
UPDATE HR.tbl_Countries SET IsoCode = N'LA' WHERE CountryID = N'113';
UPDATE HR.tbl_Countries SET IsoCode = N'LB' WHERE CountryID = N'116';
UPDATE HR.tbl_Countries SET IsoCode = N'LC' WHERE CountryID = N'176';
UPDATE HR.tbl_Countries SET IsoCode = N'LI' WHERE CountryID = N'119';
UPDATE HR.tbl_Countries SET IsoCode = N'LK' WHERE CountryID = N'185';
UPDATE HR.tbl_Countries SET IsoCode = N'LR' WHERE CountryID = N'117';
UPDATE HR.tbl_Countries SET IsoCode = N'LS' WHERE CountryID = N'114';
UPDATE HR.tbl_Countries SET IsoCode = N'LT' WHERE CountryID = N'120';
UPDATE HR.tbl_Countries SET IsoCode = N'LU' WHERE CountryID = N'121';
UPDATE HR.tbl_Countries SET IsoCode = N'LV' WHERE CountryID = N'115';
UPDATE HR.tbl_Countries SET IsoCode = N'LY' WHERE CountryID = N'118';
UPDATE HR.tbl_Countries SET IsoCode = N'MA' WHERE CountryID = N'129';
UPDATE HR.tbl_Countries SET IsoCode = N'MC' WHERE CountryID = N'136';
UPDATE HR.tbl_Countries SET IsoCode = N'MD' WHERE CountryID = N'135';
UPDATE HR.tbl_Countries SET IsoCode = N'ME' WHERE CountryID = N'138';
UPDATE HR.tbl_Countries SET IsoCode = N'MG' WHERE CountryID = N'123';
UPDATE HR.tbl_Countries SET IsoCode = N'MH' WHERE CountryID = N'099';
UPDATE HR.tbl_Countries SET IsoCode = N'MK' WHERE CountryID = N'122';
UPDATE HR.tbl_Countries SET IsoCode = N'ML' WHERE CountryID = N'127';
UPDATE HR.tbl_Countries SET IsoCode = N'MM' WHERE CountryID = N'141';
UPDATE HR.tbl_Countries SET IsoCode = N'MN' WHERE CountryID = N'137';
UPDATE HR.tbl_Countries SET IsoCode = N'MP' WHERE CountryID = N'128';
UPDATE HR.tbl_Countries SET IsoCode = N'MQ' WHERE CountryID = N'130';
UPDATE HR.tbl_Countries SET IsoCode = N'MR' WHERE CountryID = N'132';
UPDATE HR.tbl_Countries SET IsoCode = N'MS' WHERE CountryID = N'139';
UPDATE HR.tbl_Countries SET IsoCode = N'MT' WHERE CountryID = N'002';
UPDATE HR.tbl_Countries SET IsoCode = N'MU' WHERE CountryID = N'131';
UPDATE HR.tbl_Countries SET IsoCode = N'MV' WHERE CountryID = N'126';
UPDATE HR.tbl_Countries SET IsoCode = N'MW' WHERE CountryID = N'125';
UPDATE HR.tbl_Countries SET IsoCode = N'MX' WHERE CountryID = N'133';
UPDATE HR.tbl_Countries SET IsoCode = N'MY' WHERE CountryID = N'124';
UPDATE HR.tbl_Countries SET IsoCode = N'MZ' WHERE CountryID = N'140';
UPDATE HR.tbl_Countries SET IsoCode = N'NA' WHERE CountryID = N'142';
UPDATE HR.tbl_Countries SET IsoCode = N'NE' WHERE CountryID = N'146';
UPDATE HR.tbl_Countries SET IsoCode = N'NG' WHERE CountryID = N'147';
UPDATE HR.tbl_Countries SET IsoCode = N'NI' WHERE CountryID = N'145';
UPDATE HR.tbl_Countries SET IsoCode = N'NL' WHERE CountryID = N'151';
UPDATE HR.tbl_Countries SET IsoCode = N'NO' WHERE CountryID = N'148';
UPDATE HR.tbl_Countries SET IsoCode = N'NP' WHERE CountryID = N'144';
UPDATE HR.tbl_Countries SET IsoCode = N'NR' WHERE CountryID = N'143';
UPDATE HR.tbl_Countries SET IsoCode = N'NZ' WHERE CountryID = N'149';
UPDATE HR.tbl_Countries SET IsoCode = N'OM' WHERE CountryID = N'150';
UPDATE HR.tbl_Countries SET IsoCode = N'PA' WHERE CountryID = N'155';
UPDATE HR.tbl_Countries SET IsoCode = N'PE' WHERE CountryID = N'158';
UPDATE HR.tbl_Countries SET IsoCode = N'PG' WHERE CountryID = N'156';
UPDATE HR.tbl_Countries SET IsoCode = N'PH' WHERE CountryID = N'069';
UPDATE HR.tbl_Countries SET IsoCode = N'PK' WHERE CountryID = N'152';
UPDATE HR.tbl_Countries SET IsoCode = N'PL' WHERE CountryID = N'159';
UPDATE HR.tbl_Countries SET IsoCode = N'PN' WHERE CountryID = N'100';
UPDATE HR.tbl_Countries SET IsoCode = N'PR' WHERE CountryID = N'219';
UPDATE HR.tbl_Countries SET IsoCode = N'PS' WHERE CountryID = N'154';
UPDATE HR.tbl_Countries SET IsoCode = N'PT' WHERE CountryID = N'160';
UPDATE HR.tbl_Countries SET IsoCode = N'PW' WHERE CountryID = N'153';
UPDATE HR.tbl_Countries SET IsoCode = N'PY' WHERE CountryID = N'157';
UPDATE HR.tbl_Countries SET IsoCode = N'QA' WHERE CountryID = N'161';
UPDATE HR.tbl_Countries SET IsoCode = N'RO' WHERE CountryID = N'168';
UPDATE HR.tbl_Countries SET IsoCode = N'RS' WHERE CountryID = N'179';
UPDATE HR.tbl_Countries SET IsoCode = N'RU' WHERE CountryID = N'169';
UPDATE HR.tbl_Countries SET IsoCode = N'RW' WHERE CountryID = N'167';
UPDATE HR.tbl_Countries SET IsoCode = N'SA' WHERE CountryID = N'008';
UPDATE HR.tbl_Countries SET IsoCode = N'SB' WHERE CountryID = N'101';
UPDATE HR.tbl_Countries SET IsoCode = N'SC' WHERE CountryID = N'180';
UPDATE HR.tbl_Countries SET IsoCode = N'SD' WHERE CountryID = N'188';
UPDATE HR.tbl_Countries SET IsoCode = N'SE' WHERE CountryID = N'189';
UPDATE HR.tbl_Countries SET IsoCode = N'SG' WHERE CountryID = N'182';
UPDATE HR.tbl_Countries SET IsoCode = N'SI' WHERE CountryID = N'064';
UPDATE HR.tbl_Countries SET IsoCode = N'SK' WHERE CountryID = N'063';
UPDATE HR.tbl_Countries SET IsoCode = N'SL' WHERE CountryID = N'181';
UPDATE HR.tbl_Countries SET IsoCode = N'SM' WHERE CountryID = N'174';
UPDATE HR.tbl_Countries SET IsoCode = N'SN' WHERE CountryID = N'178';
UPDATE HR.tbl_Countries SET IsoCode = N'SO' WHERE CountryID = N'184';
UPDATE HR.tbl_Countries SET IsoCode = N'ST' WHERE CountryID = N'177';
UPDATE HR.tbl_Countries SET IsoCode = N'SV' WHERE CountryID = N'059';
UPDATE HR.tbl_Countries SET IsoCode = N'SY' WHERE CountryID = N'183';
UPDATE HR.tbl_Countries SET IsoCode = N'SZ' WHERE CountryID = N'186';
UPDATE HR.tbl_Countries SET IsoCode = N'TD' WHERE CountryID = N'040';
UPDATE HR.tbl_Countries SET IsoCode = N'TG' WHERE CountryID = N'197';
UPDATE HR.tbl_Countries SET IsoCode = N'TH' WHERE CountryID = N'191';
UPDATE HR.tbl_Countries SET IsoCode = N'TJ' WHERE CountryID = N'194';
UPDATE HR.tbl_Countries SET IsoCode = N'TL' WHERE CountryID = N'196';
UPDATE HR.tbl_Countries SET IsoCode = N'TM' WHERE CountryID = N'201';
UPDATE HR.tbl_Countries SET IsoCode = N'TN' WHERE CountryID = N'200';
UPDATE HR.tbl_Countries SET IsoCode = N'TO' WHERE CountryID = N'198';
UPDATE HR.tbl_Countries SET IsoCode = N'TR' WHERE CountryID = N'202';
UPDATE HR.tbl_Countries SET IsoCode = N'TT' WHERE CountryID = N'199';
UPDATE HR.tbl_Countries SET IsoCode = N'TV' WHERE CountryID = N'203';
UPDATE HR.tbl_Countries SET IsoCode = N'TW' WHERE CountryID = N'192';
UPDATE HR.tbl_Countries SET IsoCode = N'TZ' WHERE CountryID = N'193';
UPDATE HR.tbl_Countries SET IsoCode = N'UA' WHERE CountryID = N'204';
UPDATE HR.tbl_Countries SET IsoCode = N'UG' WHERE CountryID = N'205';
UPDATE HR.tbl_Countries SET IsoCode = N'US' WHERE CountryID = N'066';
UPDATE HR.tbl_Countries SET IsoCode = N'UY' WHERE CountryID = N'206';
UPDATE HR.tbl_Countries SET IsoCode = N'UZ' WHERE CountryID = N'207';
UPDATE HR.tbl_Countries SET IsoCode = N'VA' WHERE CountryID = N'209';
UPDATE HR.tbl_Countries SET IsoCode = N'VC' WHERE CountryID = N'175';
UPDATE HR.tbl_Countries SET IsoCode = N'VE' WHERE CountryID = N'210';
UPDATE HR.tbl_Countries SET IsoCode = N'VN' WHERE CountryID = N'211';
UPDATE HR.tbl_Countries SET IsoCode = N'VU' WHERE CountryID = N'208';
UPDATE HR.tbl_Countries SET IsoCode = N'WF' WHERE CountryID = N'212';
UPDATE HR.tbl_Countries SET IsoCode = N'WS' WHERE CountryID = N'172';
UPDATE HR.tbl_Countries SET IsoCode = N'XK' WHERE CountryID = N'111';
UPDATE HR.tbl_Countries SET IsoCode = N'YE' WHERE CountryID = N'213';
UPDATE HR.tbl_Countries SET IsoCode = N'ZA' WHERE CountryID = N'187';
UPDATE HR.tbl_Countries SET IsoCode = N'ZM' WHERE CountryID = N'215';
UPDATE HR.tbl_Countries SET IsoCode = N'ZW' WHERE CountryID = N'216';
-- Países que faltaban (nombre en español ISO; sin nacionalidad: no existe en la fuente)
INSERT INTO HR.tbl_Countries (CountryID, CountryName, Nationality, NationalityCode, AuxSIITH, AuxCEAACES, CreatedAt, IsoCode, Source, IsActive) VALUES
(N'234', N'ANTIGUA Y BARBUDA', NULL, NULL, NULL, NULL, SYSDATETIME(), N'AG', N'ISO', 1),
(N'235', N'ANGUILA', NULL, NULL, NULL, NULL, SYSDATETIME(), N'AI', N'ISO', 1),
(N'236', N'ANTÁRTIDA', NULL, NULL, NULL, NULL, SYSDATETIME(), N'AQ', N'ISO', 1),
(N'237', N'ISLAS ÄLAND', NULL, NULL, NULL, NULL, SYSDATETIME(), N'AX', N'ISO', 1),
(N'238', N'AZERBAIYÁN', NULL, NULL, NULL, NULL, SYSDATETIME(), N'AZ', N'ISO', 1),
(N'239', N'BANGLADÉS', NULL, NULL, NULL, NULL, SYSDATETIME(), N'BD', N'ISO', 1),
(N'240', N'BARÉIN', NULL, NULL, NULL, NULL, SYSDATETIME(), N'BH', N'ISO', 1),
(N'241', N'SAN BARTOLOMÉ', NULL, NULL, NULL, NULL, SYSDATETIME(), N'BL', N'ISO', 1),
(N'242', N'ISLAS BES (CARIBE NEERLANDÉS)', NULL, NULL, NULL, NULL, SYSDATETIME(), N'BQ', N'ISO', 1),
(N'243', N'ISLA BOUVET', NULL, NULL, NULL, NULL, SYSDATETIME(), N'BV', N'ISO', 1),
(N'244', N'ISLAS COCOS (KEELING)', NULL, NULL, NULL, NULL, SYSDATETIME(), N'CC', N'ISO', 1),
(N'245', N'ISLAS COOK', NULL, NULL, NULL, NULL, SYSDATETIME(), N'CK', N'ISO', 1),
(N'246', N'ISLA DE NAVIDAD', NULL, NULL, NULL, NULL, SYSDATETIME(), N'CX', N'ISO', 1),
(N'247', N'ISLAS MALVINAS', NULL, NULL, NULL, NULL, SYSDATETIME(), N'FK', N'ISO', 1),
(N'248', N'ISLAS FEROE', NULL, NULL, NULL, NULL, SYSDATETIME(), N'FO', N'ISO', 1),
(N'249', N'GUERNSEY', NULL, NULL, NULL, NULL, SYSDATETIME(), N'GG', N'ISO', 1),
(N'250', N'GIBRALTAR', NULL, NULL, NULL, NULL, SYSDATETIME(), N'GI', N'ISO', 1),
(N'251', N'GROENLANDIA', NULL, NULL, NULL, NULL, SYSDATETIME(), N'GL', N'ISO', 1),
(N'252', N'GUADALUPE', NULL, NULL, NULL, NULL, SYSDATETIME(), N'GP', N'ISO', 1),
(N'253', N'ISLAS GEORGIAS DEL SUR Y SÁNDWICH DEL SUR', NULL, NULL, NULL, NULL, SYSDATETIME(), N'GS', N'ISO', 1),
(N'254', N'ISLA HEARD E ISLAS MCDONALD', NULL, NULL, NULL, NULL, SYSDATETIME(), N'HM', N'ISO', 1),
(N'255', N'ISLA DE MAN', NULL, NULL, NULL, NULL, SYSDATETIME(), N'IM', N'ISO', 1),
(N'256', N'TERRITORIO BRITÁNICO DEL OCÉANO ÍNDICO', NULL, NULL, NULL, NULL, SYSDATETIME(), N'IO', N'ISO', 1),
(N'257', N'JERSEY', NULL, NULL, NULL, NULL, SYSDATETIME(), N'JE', N'ISO', 1),
(N'258', N'SAN MARTÍN (ZONA FRANCESA)', NULL, NULL, NULL, NULL, SYSDATETIME(), N'MF', N'ISO', 1),
(N'259', N'MACAO', NULL, NULL, NULL, NULL, SYSDATETIME(), N'MO', N'ISO', 1),
(N'260', N'NUEVA CALEDONIA', NULL, NULL, NULL, NULL, SYSDATETIME(), N'NC', N'ISO', 1),
(N'261', N'ISLA NORFOLK', NULL, NULL, NULL, NULL, SYSDATETIME(), N'NF', N'ISO', 1),
(N'262', N'NIUE', NULL, NULL, NULL, NULL, SYSDATETIME(), N'NU', N'ISO', 1),
(N'263', N'POLINESIA FRANCESA', NULL, NULL, NULL, NULL, SYSDATETIME(), N'PF', N'ISO', 1),
(N'264', N'SAN PEDRO Y MIQUELON', NULL, NULL, NULL, NULL, SYSDATETIME(), N'PM', N'ISO', 1),
(N'265', N'REUNIÓN', NULL, NULL, NULL, NULL, SYSDATETIME(), N'RE', N'ISO', 1),
(N'266', N'SANTA ELENA, ASCENSIÓN Y TRISTÁN DE ACUÑA', NULL, NULL, NULL, NULL, SYSDATETIME(), N'SH', N'ISO', 1),
(N'267', N'SVALBARD Y JAN MAYEN', NULL, NULL, NULL, NULL, SYSDATETIME(), N'SJ', N'ISO', 1),
(N'268', N'SURINÁM', NULL, NULL, NULL, NULL, SYSDATETIME(), N'SR', N'ISO', 1),
(N'269', N'SUDÁN DEL SUR', NULL, NULL, NULL, NULL, SYSDATETIME(), N'SS', N'ISO', 1),
(N'270', N'ISLA DE SAN MARTÍN (ZONA HOLANDESA)', NULL, NULL, NULL, NULL, SYSDATETIME(), N'SX', N'ISO', 1),
(N'271', N'ISLAS TURCAS Y CAICOS', NULL, NULL, NULL, NULL, SYSDATETIME(), N'TC', N'ISO', 1),
(N'272', N'TERRITORIOS FRANCESES DEL SUR', NULL, NULL, NULL, NULL, SYSDATETIME(), N'TF', N'ISO', 1),
(N'273', N'TOKELAU', NULL, NULL, NULL, NULL, SYSDATETIME(), N'TK', N'ISO', 1),
(N'274', N'ISLAS ULTRAMARINAS MENORES DE ESTADOS UNIDOS', NULL, NULL, NULL, NULL, SYSDATETIME(), N'UM', N'ISO', 1),
(N'275', N'ISLAS VIRGENES BRITANICAS', NULL, NULL, NULL, NULL, SYSDATETIME(), N'VG', N'ISO', 1),
(N'276', N'ISLAS VIRGENES DE ESTADOS UNIDOS', NULL, NULL, NULL, NULL, SYSDATETIME(), N'VI', N'ISO', 1),
(N'277', N'MAYOTTE', NULL, NULL, NULL, NULL, SYSDATETIME(), N'YT', N'ISO', 1);
IF (SELECT COUNT(*) FROM HR.tbl_Countries) <> 250 BEGIN ROLLBACK; THROW 50000, 'El conteo final de paises no coincide con lo esperado', 1; END
COMMIT;
SELECT (SELECT COUNT(*) FROM HR.tbl_Countries) paises, (SELECT COUNT(*) FROM HR.tbl_Countries WHERE IsoCode IS NOT NULL) conIso, (SELECT COUNT(*) FROM HR.tbl_GeoCleanupLog) registrosLog;
