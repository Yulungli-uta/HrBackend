/* ============================================================================
   36_document_types_anulacion_renuncia.sql
   Tipos de documento (HR.ref_Types, Category = 'DOCUMENT_TYPE') para el oficio de anulación y el de renuncia
   de contratos y acciones de personal.

   El backend (EntityDocumentLayoutPolicy) decide la carpeta del archivo por el rol o, si el cliente no manda rol, por el
   nombre del tipo de documento: un nombre que contiene "ANULA" va a {año}/anulacion/ y uno que contiene "RENUNCIA" va a
   {año}/renuncia/. Con estos dos tipos el usuario los elige en la lista de tipos que ya existe en las pantallas de
   Contratos y Acciones, sin cambios en el frontend.

   Idempotente (por Category + Name). Ejecutar con sqlcmd -f 65001.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = 'DOCUMENT_TYPE' AND Name = N'OFICIO_ANULACION')
    INSERT HR.ref_Types (Category, Name, Description, IsActive, SortOrder, Metadata)
    VALUES ('DOCUMENT_TYPE', N'OFICIO_ANULACION', N'Oficio o documento de anulación', 1, 30, N'{"requiresReference":false}');
GO

IF NOT EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = 'DOCUMENT_TYPE' AND Name = N'OFICIO_RENUNCIA')
    INSERT HR.ref_Types (Category, Name, Description, IsActive, SortOrder, Metadata)
    VALUES ('DOCUMENT_TYPE', N'OFICIO_RENUNCIA', N'Documento de renuncia', 1, 40, N'{"requiresReference":false}');
GO
