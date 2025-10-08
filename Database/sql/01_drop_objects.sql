
-- === DROP OBJECTS FOR SCHEMA HR (safe-order) ===
SET NOCOUNT ON;

-- 1) Drop views
DECLARE @sql NVARCHAR(MAX)='';
SELECT @sql = STRING_AGG(CONCAT('IF OBJECT_ID(''', QUOTENAME(s.name)+'.'+QUOTENAME(v.name), ''',''V'') IS NOT NULL DROP VIEW ', QUOTENAME(s.name)+'.'+QUOTENAME(v.name), ';'), CHAR(10))
FROM sys.views v
JOIN sys.schemas s ON v.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 2) Drop triggers (table and database level) in HR
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('DROP TRIGGER ', QUOTENAME(s.name),'.',QUOTENAME(t.name),';'), CHAR(10))
FROM sys.triggers t
JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 3) Drop stored procedures
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('IF OBJECT_ID(''', QUOTENAME(s.name)+'.'+QUOTENAME(p.name), ''',''P'') IS NOT NULL DROP PROCEDURE ', QUOTENAME(s.name),'.',QUOTENAME(p.name), ';'), CHAR(10))
FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 4) Drop functions
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('IF OBJECT_ID(''', QUOTENAME(s.name)+'.'+QUOTENAME(o.name), ''',''FN'') IS NOT NULL DROP FUNCTION ', QUOTENAME(s.name),'.',QUOTENAME(o.name), ';'), CHAR(10))
FROM sys.objects o
JOIN sys.schemas s ON o.schema_id=s.schema_id
WHERE s.name='HR' AND o.[type] IN ('FN','IF','TF');
EXEC(@sql);

-- 5) Drop foreign keys
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('ALTER TABLE ', QUOTENAME(SCHEMA_NAME(t.schema_id)),'.',QUOTENAME(t.name), ' DROP CONSTRAINT ', QUOTENAME(fk.name), ';'), CHAR(10))
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id=t.object_id
JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);

-- 6) Drop tables
SET @sql='';
SELECT @sql = STRING_AGG(CONCAT('DROP TABLE ', QUOTENAME(s.name),'.',QUOTENAME(t.name), ';'), CHAR(10))
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name='HR';
EXEC(@sql);
GO
