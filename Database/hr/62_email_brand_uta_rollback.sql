-- Reversión de 62_email_brand_uta.sql: restaura layouts y plantillas anteriores.
SET NOCOUNT ON;
BEGIN TRAN;
UPDATE HR.tbl_EmailLayouts SET HeaderHtml = N'<div style="background-color:#6d1313;padding:20px 24px;">
  <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
    <tr>
      <td style="vertical-align:middle;">
        <h2 style="
          margin:0;
          font-family:Arial, Helvetica, sans-serif;
          font-size:20px;
          font-weight:600;
          color:#ffffff;
        ">
          Sistema Integrado UTA
        </h2>

        <p style="
          margin:4px 0 0 0;
          font-family:Arial, Helvetica, sans-serif;
          font-size:13px;
          color:#ffffff;
          opacity:0.9;
        ">
          Notificación automática
        </p>
      </td>
    </tr>
  </table>
</div>
', FooterHtml = N'<div style="background-color:#f9fafb;padding:16px 24px;border-top:1px solid #e5e7eb;">
  <p style="
    margin:0;
    font-family:Arial, Helvetica, sans-serif;
    font-size:12px;
    color:#6b7280;
    line-height:1.5;
  ">
    Este es un correo generado automáticamente por el Sistema Integrado UTA.
    <br>
    Por favor, no responda a este mensaje.
  </p>

  <p style="
    margin:8px 0 0 0;
    font-family:Arial, Helvetica, sans-serif;
    font-size:11px;
    color:#9ca3af;
  ">
    © Universidad Técnica de Ambato
  </p>
</div>
', UpdatedAt = SYSDATETIME() WHERE Slug = N'informativo';
UPDATE HR.tbl_EmailLayouts SET HeaderHtml = N'
<div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden">
  <div style="background:#7a1f2b;color:#fff;padding:20px 24px">
    <div style="font-size:20px;font-weight:700">Universidad Técnica de Ambato</div>
    <div style="font-size:14px;margin-top:4px">Sistema Institucional de Firma Electrónica</div>
  </div>
  <div style="padding:24px;color:#1f2937">', FooterHtml = N'
  </div>
  <div style="background:#f3f4f6;padding:16px 24px;color:#4b5563;font-size:12px">
    Mensaje generado automáticamente por la Universidad Técnica de Ambato.
    Verifique la validez del documento desde el portal institucional.
  </div>
</div>', UpdatedAt = SYSDATETIME() WHERE Slug = N'firma-electronica-final';
UPDATE HR.TBL_PARAMETERS SET Pvalues = N'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;">
  <div style="text-align: center; margin-bottom: 24px;">
    <img src="https://www.uta.edu.ec/v3.2/img/logo_uta.png" alt="Universidad Técnica de Ambato" style="height: 60px;" />
  </div>
  <h2 style="color: #003087; margin-top: 0;">Bienvenido/a a la Universidad Técnica de Ambato</h2>
  <p>Estimado/a <strong>{FirstName}</strong>,</p>
  <p>Nos complace informarle que su cuenta institucional ha sido creada exitosamente como parte del proceso de suscripción de su contrato:</p>
  <table style="width: 100%; border-collapse: collapse; margin: 16px 0;">
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; width: 40%; border: 1px solid #ddd;">Usuario (correo institucional)</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd;">{InstitutionalEmail}</td>
    </tr>
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; border: 1px solid #ddd;">Contraseña temporal</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd; font-family: monospace; font-size: 15px;">{InitialPassword}</td>
    </tr>
  </table>
  <p style="background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px 14px; margin: 16px 0;">
    <strong>Importante:</strong> Deberá cambiar su contraseña en el primer inicio de sesión por razones de seguridad.
  </p>
  <p>Para ingresar al sistema utilice el portal institucional con las credenciales indicadas arriba.</p>
  <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
  <p style="font-size: 12px; color: #666;">
    Si tiene alguna inconveniencia, comuníquese con el Departamento de Talento Humano o con la Unidad de Tecnologías de la Información.<br />
    Este mensaje fue generado automáticamente — por favor no responda a este correo.
  </p>
</div>' WHERE Name = N'EMAIL_TEMPLATE_ACCOUNT_CREATED_CONTRACT';
UPDATE HR.TBL_PARAMETERS SET Pvalues = N'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;">
  <div style="text-align: center; margin-bottom: 24px;">
    <img src="https://www.uta.edu.ec/v3.2/img/logo_uta.png" alt="Universidad Técnica de Ambato" style="height: 60px;" />
  </div>
  <h2 style="color: #003087; margin-top: 0;">Bienvenido/a a la Universidad Técnica de Ambato</h2>
  <p>Estimado/a <strong>{FirstName}</strong>,</p>
  <p>Nos complace informarle que su cuenta institucional ha sido creada exitosamente como parte del proceso de su acción de personal:</p>
  <table style="width: 100%; border-collapse: collapse; margin: 16px 0;">
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; width: 40%; border: 1px solid #ddd;">Usuario (correo institucional)</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd;">{InstitutionalEmail}</td>
    </tr>
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; border: 1px solid #ddd;">Contraseña temporal</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd; font-family: monospace; font-size: 15px;">{InitialPassword}</td>
    </tr>
  </table>
  <p style="background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px 14px; margin: 16px 0;">
    <strong>Importante:</strong> Deberá cambiar su contraseña en el primer inicio de sesión por razones de seguridad.
  </p>
  <p>Para ingresar al sistema utilice el portal institucional con las credenciales indicadas arriba.</p>
  <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
  <p style="font-size: 12px; color: #666;">
    Si tiene alguna inconveniencia, comuníquese con el Departamento de Talento Humano o con la Unidad de Tecnologías de la Información.<br />
    Este mensaje fue generado automáticamente — por favor no responda a este correo.
  </p>
</div>' WHERE Name = N'EMAIL_TEMPLATE_ACCOUNT_CREATED_ACTION';
COMMIT;
