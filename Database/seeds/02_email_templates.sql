-- ============================================================
-- SEED: Templates de Email
-- Fuentes: seed_email_template_contract.sql + seed_email_template_action.sql
-- Generado: 2026-05-29
-- ============================================================

-- ---- Templates de email - Contratos ----
-- Plantilla de correo de bienvenida para cuenta institucional creada vÃ­a contrato
-- Tabla destino: HR.TBL_PARAMETERS
-- Placeholders disponibles: {FirstName}, {InstitutionalEmail}, {InitialPassword}
-- Ejecutar una sola vez; si ya existe, actualizar Pvalues.

MERGE INTO HR.TBL_PARAMETERS AS tgt
USING (
    SELECT
        N'EMAIL_TEMPLATE_ACCOUNT_CREATED_CONTRACT' AS Name,
        N'HTML' AS DataType,
        N'Plantilla HTML para notificar al empleado que su cuenta institucional fue creada al firmar un contrato.' AS Description,
        N'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;">
  <div style="text-align: center; margin-bottom: 24px;">
    <img src="https://www.uta.edu.ec/v3.2/img/logo_uta.png" alt="Universidad TÃ©cnica de Ambato" style="height: 60px;" />
  </div>
  <h2 style="color: #003087; margin-top: 0;">Bienvenido/a a la Universidad TÃ©cnica de Ambato</h2>
  <p>Estimado/a <strong>{FirstName}</strong>,</p>
  <p>Nos complace informarle que su cuenta institucional ha sido creada exitosamente como parte del proceso de suscripciÃ³n de su contrato:</p>
  <table style="width: 100%; border-collapse: collapse; margin: 16px 0;">
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; width: 40%; border: 1px solid #ddd;">Usuario (correo institucional)</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd;">{InstitutionalEmail}</td>
    </tr>
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; border: 1px solid #ddd;">ContraseÃ±a temporal</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd; font-family: monospace; font-size: 15px;">{InitialPassword}</td>
    </tr>
  </table>
  <p style="background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px 14px; margin: 16px 0;">
    <strong>Importante:</strong> DeberÃ¡ cambiar su contraseÃ±a en el primer inicio de sesiÃ³n por razones de seguridad.
  </p>
  <p>Para ingresar al sistema utilice el portal institucional con las credenciales indicadas arriba.</p>
  <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
  <p style="font-size: 12px; color: #666;">
    Si tiene alguna inconveniencia, comunÃ­quese con el Departamento de Talento Humano o con la Unidad de TecnologÃ­as de la InformaciÃ³n.<br />
    Este mensaje fue generado automÃ¡ticamente â€” por favor no responda a este correo.
  </p>
</div>' AS Pvalues,
        1 AS IsActive,
        GETDATE() AS CreatedAt,
        1 AS CreatedBy
) AS src ON tgt.Name = src.Name
WHEN MATCHED THEN
    UPDATE SET
        tgt.Pvalues      = src.Pvalues,
        tgt.Description  = src.Description,
        tgt.DataType     = src.DataType,
        tgt.IsActive     = src.IsActive,
        tgt.UpdatedAt    = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Name, DataType, Description, Pvalues, IsActive, CreatedAt, CreatedBy)
    VALUES (src.Name, src.DataType, src.Description, src.Pvalues, src.IsActive, src.CreatedAt, src.CreatedBy);


-- ---- Templates de email - Acción de Personal ----
-- Plantilla de correo de bienvenida para cuenta institucional creada vÃ­a acciÃ³n de personal
-- Tabla destino: HR.TBL_PARAMETERS
-- Placeholders disponibles: {FirstName}, {InstitutionalEmail}, {InitialPassword}
-- Ejecutar una sola vez; si ya existe, actualizar Pvalues.

MERGE INTO HR.TBL_PARAMETERS AS tgt
USING (
    SELECT
        N'EMAIL_TEMPLATE_ACCOUNT_CREATED_ACTION' AS Name,
        N'HTML' AS DataType,
        N'Plantilla HTML para notificar al empleado que su cuenta institucional fue creada al firmar una acciÃ³n de personal.' AS Description,
        N'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;">
  <div style="text-align: center; margin-bottom: 24px;">
    <img src="https://www.uta.edu.ec/v3.2/img/logo_uta.png" alt="Universidad TÃ©cnica de Ambato" style="height: 60px;" />
  </div>
  <h2 style="color: #003087; margin-top: 0;">Bienvenido/a a la Universidad TÃ©cnica de Ambato</h2>
  <p>Estimado/a <strong>{FirstName}</strong>,</p>
  <p>Nos complace informarle que su cuenta institucional ha sido creada exitosamente como parte del proceso de su acciÃ³n de personal:</p>
  <table style="width: 100%; border-collapse: collapse; margin: 16px 0;">
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; width: 40%; border: 1px solid #ddd;">Usuario (correo institucional)</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd;">{InstitutionalEmail}</td>
    </tr>
    <tr>
      <td style="padding: 10px 12px; background: #f5f5f5; font-weight: bold; border: 1px solid #ddd;">ContraseÃ±a temporal</td>
      <td style="padding: 10px 12px; border: 1px solid #ddd; font-family: monospace; font-size: 15px;">{InitialPassword}</td>
    </tr>
  </table>
  <p style="background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px 14px; margin: 16px 0;">
    <strong>Importante:</strong> DeberÃ¡ cambiar su contraseÃ±a en el primer inicio de sesiÃ³n por razones de seguridad.
  </p>
  <p>Para ingresar al sistema utilice el portal institucional con las credenciales indicadas arriba.</p>
  <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
  <p style="font-size: 12px; color: #666;">
    Si tiene alguna inconveniencia, comunÃ­quese con el Departamento de Talento Humano o con la Unidad de TecnologÃ­as de la InformaciÃ³n.<br />
    Este mensaje fue generado automÃ¡ticamente â€” por favor no responda a este correo.
  </p>
</div>' AS Pvalues,
        1 AS IsActive,
        GETDATE() AS CreatedAt,
        1 AS CreatedBy
) AS src ON tgt.Name = src.Name
WHEN MATCHED THEN
    UPDATE SET
        tgt.Pvalues      = src.Pvalues,
        tgt.Description  = src.Description,
        tgt.DataType     = src.DataType,
        tgt.IsActive     = src.IsActive,
        tgt.UpdatedAt    = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Name, DataType, Description, Pvalues, IsActive, CreatedAt, CreatedBy)
    VALUES (src.Name, src.DataType, src.Description, src.Pvalues, src.IsActive, src.CreatedAt, src.CreatedBy);

