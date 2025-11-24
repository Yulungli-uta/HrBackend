# Imágenes para Reportes

## Archivos Requeridos

Coloca las siguientes imágenes en este directorio:

### 1. header.png
- **Dimensiones recomendadas**: 2480 x 200 píxeles
- **Formato**: PNG con transparencia
- **Contenido**: Logo de UTA + Nombre de la institución + Encabezado
- **Ejemplo**: 
  ```
  [Logo UTA]  UNIVERSIDAD TÉCNICA DE AMBATO
              Sistema de Recursos Humanos
  ```

### 2. footer.png
- **Dimensiones recomendadas**: 2480 x 100 píxeles
- **Formato**: PNG con transparencia
- **Contenido**: Información de contacto + Dirección + Teléfono
- **Ejemplo**:
  ```
  Av. Los Chasquis y Río Payamino | Ambato - Ecuador
  Teléfono: (03) 2521-081 | www.uta.edu.ec
  ```

## Notas

- Si no existen estas imágenes, el sistema usará texto plano como fallback
- Las imágenes se escalan automáticamente para ajustarse al ancho de la página
- Usar PNG con fondo transparente para mejor calidad
- Colores recomendados: Azul oscuro (#003366) y blanco

## Configuración

Las rutas se configuran en `appsettings.json`:

```json
{
  "ReportSettings": {
    "HeaderImagePath": "wwwroot/images/reports/header.png",
    "FooterImagePath": "wwwroot/images/reports/footer.png"
  }
}
```
