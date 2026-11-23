-- Correos institucionales con la identidad UTA 2026 (azul #00345e, filete rojo #ab171f, pie con datos del campus).
-- Cambia SOLO datos: HR.tbl_EmailLayouts (informativo, firma-electronica-final) y 2 plantillas de HR.TBL_PARAMETERS.
-- Las imágenes remotas (logo uta.edu.ec) se reemplazan por un encabezado de texto: los clientes de correo bloquean imágenes externas.
-- Reversión: 62_email_brand_uta_rollback.sql (generado con los valores anteriores).
SET NOCOUNT ON;
BEGIN TRAN;
UPDATE HR.tbl_EmailLayouts SET HeaderHtml = N'<div style="background-color:#00345e;padding:18px 24px;border-bottom:4px solid #ab171f;">
  <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
    <tr>
      <td style="vertical-align:middle;">
        <div style="font-family:Georgia,''Times New Roman'',serif;font-size:13px;letter-spacing:1.5px;line-height:1.3;color:#ffffff;text-transform:uppercase;">Universidad<br>T&eacute;cnica de<br>Ambato</div>
      </td>
      <td style="vertical-align:middle;text-align:right;">
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:700;color:#ffffff;">Sistema Integrado UTA</div>
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#d6e2ee;margin-top:2px;">Notificaci&oacute;n autom&aacute;tica</div>
      </td>
    </tr>
  </table>
</div>
', FooterHtml = N'<div style="background-color:#f3f6f9;padding:16px 24px;border-top:1px solid #d9e1ea;">
  <p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#536475;line-height:1.5;">Este es un correo generado autom&aacute;ticamente por el Sistema Integrado UTA.<br>Por favor, no responda a este mensaje.</p>
  <p style="margin:8px 0 0 0;font-family:Arial,Helvetica,sans-serif;font-size:11px;color:#7a8896;line-height:1.5;">Campus Huachi &middot; Av. Los Chasquis entre R&iacute;o Guayllabamba y R&iacute;o Payamino &middot; 03-3700090 &middot; <a href="https://uta.edu.ec" style="color:#00345e;text-decoration:none;font-weight:700;">uta.edu.ec</a></p>
</div>
', UpdatedAt = SYSDATETIME() WHERE Slug = N'informativo';
IF @@ROWCOUNT <> 1 BEGIN ROLLBACK; RAISERROR('Layout no encontrado: %s', 16, 1, N'informativo'); RETURN; END
UPDATE HR.tbl_EmailLayouts SET HeaderHtml = N'<div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;border:1px solid #d9e1ea;border-radius:8px;overflow:hidden">
<div style="background-color:#00345e;padding:18px 24px;border-bottom:4px solid #ab171f;">
  <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
    <tr>
      <td style="vertical-align:middle;">
        <div style="font-family:Georgia,''Times New Roman'',serif;font-size:13px;letter-spacing:1.5px;line-height:1.3;color:#ffffff;text-transform:uppercase;">Universidad<br>T&eacute;cnica de<br>Ambato</div>
      </td>
      <td style="vertical-align:middle;text-align:right;">
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:700;color:#ffffff;">Firma Electr&oacute;nica</div>
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#d6e2ee;margin-top:2px;">Sistema Institucional de Firma Electr&oacute;nica</div>
      </td>
    </tr>
  </table>
</div>
  <div style="padding:24px;color:#1c2630">', FooterHtml = N'  </div>
<div style="background-color:#f3f6f9;padding:16px 24px;border-top:1px solid #d9e1ea;">
  <p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#536475;line-height:1.5;">Mensaje generado autom&aacute;ticamente por la Universidad T&eacute;cnica de Ambato. Verifique la validez del documento desde el portal institucional.</p>
  <p style="margin:8px 0 0 0;font-family:Arial,Helvetica,sans-serif;font-size:11px;color:#7a8896;line-height:1.5;">Campus Huachi &middot; Av. Los Chasquis entre R&iacute;o Guayllabamba y R&iacute;o Payamino &middot; 03-3700090 &middot; <a href="https://uta.edu.ec" style="color:#00345e;text-decoration:none;font-weight:700;">uta.edu.ec</a></p>
</div>
</div>', UpdatedAt = SYSDATETIME() WHERE Slug = N'firma-electronica-final';
IF @@ROWCOUNT <> 1 BEGIN ROLLBACK; RAISERROR('Layout no encontrado: %s', 16, 1, N'firma-electronica-final'); RETURN; END
UPDATE HR.TBL_PARAMETERS SET Pvalues = N'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;">
  <div style="margin:-24px -24px 24px -24px;"><div style="background-color:#00345e;padding:18px 24px;border-bottom:4px solid #ab171f;">
  <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
    <tr>
      <td style="vertical-align:middle;">
        <div style="font-family:Georgia,''Times New Roman'',serif;font-size:13px;letter-spacing:1.5px;line-height:1.3;color:#ffffff;text-transform:uppercase;">Universidad<br>T&eacute;cnica de<br>Ambato</div>
      </td>
      <td style="vertical-align:middle;text-align:right;">
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:700;color:#ffffff;">Talento Humano</div>
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#d6e2ee;margin-top:2px;">Notificaci&oacute;n institucional</div>
      </td>
    </tr>
  </table>
</div></div>
  <h2 style="color: #00345e; margin-top: 0;">Bienvenido/a a la Universidad Técnica de Ambato</h2>
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
  <p style="background: #fdf3e1; border-left: 4px solid #a35d05; padding: 10px 14px; margin: 16px 0;">
    <strong>Importante:</strong> Deberá cambiar su contraseña en el primer inicio de sesión por razones de seguridad.
  </p>
  <p>Para ingresar al sistema utilice el portal institucional con las credenciales indicadas arriba.</p>
  <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
  <p style="font-size: 12px; color: #666;">
    Si tiene alguna inconveniencia, comuníquese con el Departamento de Talento Humano o con la Unidad de Tecnologías de la Información.<br />
    Este mensaje fue generado automáticamente — por favor no responda a este correo.
  </p>
</div>' WHERE Name = N'EMAIL_TEMPLATE_ACCOUNT_CREATED_CONTRACT';
IF @@ROWCOUNT <> 1 BEGIN ROLLBACK; RAISERROR('Plantilla no encontrada: %s', 16, 1, N'EMAIL_TEMPLATE_ACCOUNT_CREATED_CONTRACT'); RETURN; END
UPDATE HR.TBL_PARAMETERS SET Pvalues = N'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;">
  <div style="margin:-24px -24px 24px -24px;"><div style="background-color:#00345e;padding:18px 24px;border-bottom:4px solid #ab171f;">
  <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
    <tr>
      <td style="vertical-align:middle;">
        <div style="font-family:Georgia,''Times New Roman'',serif;font-size:13px;letter-spacing:1.5px;line-height:1.3;color:#ffffff;text-transform:uppercase;">Universidad<br>T&eacute;cnica de<br>Ambato</div>
      </td>
      <td style="vertical-align:middle;text-align:right;">
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:700;color:#ffffff;">Talento Humano</div>
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#d6e2ee;margin-top:2px;">Notificaci&oacute;n institucional</div>
      </td>
    </tr>
  </table>
</div></div>
  <h2 style="color: #00345e; margin-top: 0;">Bienvenido/a a la Universidad Técnica de Ambato</h2>
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
  <p style="background: #fdf3e1; border-left: 4px solid #a35d05; padding: 10px 14px; margin: 16px 0;">
    <strong>Importante:</strong> Deberá cambiar su contraseña en el primer inicio de sesión por razones de seguridad.
  </p>
  <p>Para ingresar al sistema utilice el portal institucional con las credenciales indicadas arriba.</p>
  <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
  <p style="font-size: 12px; color: #666;">
    Si tiene alguna inconveniencia, comuníquese con el Departamento de Talento Humano o con la Unidad de Tecnologías de la Información.<br />
    Este mensaje fue generado automáticamente — por favor no responda a este correo.
  </p>
</div>' WHERE Name = N'EMAIL_TEMPLATE_ACCOUNT_CREATED_ACTION';
IF @@ROWCOUNT <> 1 BEGIN ROLLBACK; RAISERROR('Plantilla no encontrada: %s', 16, 1, N'EMAIL_TEMPLATE_ACCOUNT_CREATED_ACTION'); RETURN; END
COMMIT;
SELECT 'layouts' AS item, COUNT(*) AS filas FROM HR.tbl_EmailLayouts WHERE HeaderHtml LIKE '%#00345e%' UNION ALL SELECT 'plantillas', COUNT(*) FROM HR.TBL_PARAMETERS WHERE CAST(Pvalues AS nvarchar(max)) LIKE '%#00345e%' AND Name LIKE 'EMAIL_TEMPLATE_%';
