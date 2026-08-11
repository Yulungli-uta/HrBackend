SET XACT_ABORT ON;
DECLARE @Header nvarchar(max)=N'
<div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden">
  <div style="background:#7a1f2b;color:#fff;padding:20px 24px">
    <div style="font-size:20px;font-weight:700">Universidad Técnica de Ambato</div>
    <div style="font-size:14px;margin-top:4px">Sistema Institucional de Firma Electrónica</div>
  </div>
  <div style="padding:24px;color:#1f2937">';
DECLARE @Footer nvarchar(max)=N'
  </div>
  <div style="background:#f3f4f6;padding:16px 24px;color:#4b5563;font-size:12px">
    Mensaje generado automáticamente por la Universidad Técnica de Ambato.
    Verifique la validez del documento desde el portal institucional.
  </div>
</div>';

MERGE [HR].[tbl_EmailLayouts] AS target
USING (SELECT N'firma-electronica-final' Slug,@Header HeaderHtml,@Footer FooterHtml) AS source
ON target.Slug=source.Slug
WHEN MATCHED THEN UPDATE SET HeaderHtml=source.HeaderHtml,FooterHtml=source.FooterHtml,IsActive=1,UpdatedAt=GETDATE()
WHEN NOT MATCHED THEN INSERT(Slug,HeaderHtml,FooterHtml,IsActive,CreatedAt)
VALUES(source.Slug,source.HeaderHtml,source.FooterHtml,1,GETDATE());
GO
